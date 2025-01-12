﻿using StereoKit;
using System;
using System.Collections.Generic;
using System.IO;

class Light
{
	public Pose pose;
	public Vec3 color;
}
enum LightMode
{
	Lights,
	Image,
}

class DemoSky : ITest
{
	string title       = "Sky Editor";
	string description = "";

	static List<Light> lights         = new List<Light>();
	static Tex         cubemap        = null;
	static bool        cubelightDirty = false;
	static string      cubemapFile    = "";

	Pose       windowPose    = new Pose(0.5f, 0, -0.5f, Quat.LookDir(-1, 0, 1));
	Pose       previewPose   = new Pose(0.9f, -0.1f, -0.1f, Quat.LookDir(-1,0,1));
	Pose       lightToolPose = new Pose(0.7f, -0.1f, -0.3f, Quat.Identity);
	LightMode  mode          = LightMode.Lights;
	Model      previewModel  = Model.FromFile("DamagedHelmet.gltf");
	Mesh       lightMesh     = Mesh.GenerateSphere(1);
	Material   lightProbeMat = Default.Material;
	Material   lightSrcMat   = new Material(Default.ShaderUnlit);

	public void Initialize() { }
	public void Shutdown() => Platform.FilePickerClose();
	public void Update()
	{
		UI.WindowBegin("Lighting Source", ref windowPose);
		UI.Label("Mode");
		UI.SameLine();
		if (UI.Radio("Lights", mode == LightMode.Lights)) mode = LightMode.Lights;
		UI.SameLine();
		if (UI.Radio("Image",  mode == LightMode.Image))  mode = LightMode.Image;

		UI.HSeparator();

		if (mode == LightMode.Lights)
		{ 
			if (UI.Button("Add"))
			{
				lights.Add(new Light { 
					pose  = new Pose(Vec3.Up*25*U.cm, Quat.LookDir(-Vec3.Forward)), 
					color = Vec3.One });
				UpdateLights();
			}

			if (UI.Button("Remove") && lights.Count > 1)
			{ 
				lights.RemoveAt(lights.Count-1);
				UpdateLights();
			}
		}

		if (mode == LightMode.Image)
		{
			if (!Platform.FilePickerVisible && UI.Button("Browse"))
				ShowPicker();
			UI.Label(cubemapFile);
		}

		UI.HSeparator();

		if (UI.Button("Print Lighting Code"))
		{
			Vec3[] c = Renderer.SkyLight.ToArray();
			string shStr = "new SphericalHarmonics(new Vec3[]{";
			for (int i = 0; i < c.Length; i++)
				shStr += $"new Vec3({c[i].x:F2}f, {c[i].y:F2}f, {c[i].z:F2}f),";
			shStr += "});";
			Log.Info(shStr);
		}

		UI.WindowEnd();

			
		UI.Handle("Preview", ref previewPose, previewModel.Bounds*0.1f);
		previewModel.Draw(previewPose.ToMatrix(0.1f));

		Hierarchy.Push(lightToolPose.ToMatrix());
		lightMesh.Draw(lightProbeMat, Matrix.TS(Vec3.Zero, 0.04f));
		if (mode == LightMode.Lights)
		{ 
			bool needsUpdate = false;
			for (int i = 0; i < lights.Count; i++) {
				needsUpdate = LightHandle(i) || needsUpdate;
			}
			if (needsUpdate)
				UpdateLights();
		}
		Hierarchy.Pop();

		if (cubelightDirty && cubemap.AssetState == AssetState.Loaded) {
			Renderer.SkyLight = cubemap.CubemapLighting;
			cubelightDirty = false;
		}

		Demo.ShowSummary(title, description);
	}

	bool LightHandle(int i)
	{
		UI.PushId("window"+i);
		bool dirty = UI.HandleBegin("Color", ref lights[i].pose, new Bounds(Vec3.One * 3 * U.cm));
		UI.LayoutArea(new Vec3(6,-3,0)*U.cm, new Vec2(10, 0)*U.cm);
		if (lights[i].pose.position.Length > 0.5f)
			lights[i].pose.position = lights[i].pose.position.Normalized * 0.5f;

		lightMesh.Draw(lightSrcMat, Matrix.TS(Vec3.Zero, 3* U.cm), Color.HSV(lights[i].color));

		float h = lights[i].color.x;
		float s = lights[i].color.y;
		float v = lights[i].color.z;
		dirty = UI.HSlider("H", ref h, 0, 1, 0, 10 * U.cm) || dirty;
		dirty = UI.HSlider("S", ref s, 0, 1, 0, 10 * U.cm) || dirty;
		dirty = UI.HSlider("V", ref v, 0, 1, 0, 10 * U.cm) || dirty;
		lights[i].color = new Vec3(h, s, v);

		UI.HandleEnd();
		Lines.Add(
			lights[i].pose.position, Vec3.Zero, 
			Color.HSV(lights[i].color) * LightIntensity(lights[i].pose.position) * 0.5f, 
			U.mm);

		UI.PopId();
		return dirty;
	}

	void ShowPicker()
	{
		Platform.FilePicker(PickerMode.Open, LoadSkyImage, null,
			".hdr", ".jpg", ".png");
	}

	void LoadSkyImage(string file)
	{
		cubemapFile = Path.GetFileName(file);
		cubemap     = Tex.FromCubemapEquirectangular(file);

		Renderer.SkyTex = cubemap;
		cubelightDirty  = true;
	}

	void UpdateLights()
	{
		SphericalHarmonics lighting = SphericalHarmonics.FromLights(lights
			.ConvertAll(a => new SHLight{ 
				directionTo = a.pose.position.Normalized,
				color       = Color.HSV(a.color) * LightIntensity(a.pose.position) })
			.ToArray());

		Renderer.SkyTex   = Tex.GenCubemap(lighting);
		Renderer.SkyLight = lighting;
	}

	float LightIntensity(Vec3 pos)
	{
		return Math.Max(0, 2 - pos.Magnitude * 4);
	}
}