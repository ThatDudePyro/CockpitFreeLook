using System;
using HarmonyLib;
using KSL.API;
using Rewired;
using UnityEngine;

namespace CockpitFreeLook
{
	[KSLMeta("CockpitFreeLook", "1.0.0", "Pyro")]
	public class CockpitFreeLook : BaseMod
	{
		private const float MaxPitch = 60f;
		private const float MousePitchScale = 0.45f;
		private const float JoystickLerpCoef = 10f;
		private const float MouseLerpCoef = 7f;
		private const float YawBonus = 30f;
		private static float _currentPitch;
		private static float _mouseCachedPitch;

		private static readonly AccessTools.FieldRef<CarX.Internal.CockpitCamera, float> LeftAngleRef =
			AccessTools.FieldRefAccess<CarX.Internal.CockpitCamera, float>("m_leftSideMaxDeltaAngle");
		private static readonly AccessTools.FieldRef<CarX.Internal.CockpitCamera, float> RightAngleRef =
			AccessTools.FieldRefAccess<CarX.Internal.CockpitCamera, float>("m_rightSideMaxDeltaAngle");

		private Harmony _harmony;

		void Awake()
		{
			try
			{
				_harmony = new Harmony("CockpitFreeLook.patch");

				var applyRotation = AccessTools.Method(typeof(CarX.Internal.CockpitCamera), "ApplyRotation", new[] { typeof(Quaternion), typeof(Transform) });
				_harmony.Patch(applyRotation, new HarmonyMethod(typeof(CockpitFreeLook), nameof(AddPitch)));

				var clampRotationY = AccessTools.Method(typeof(CarX.Internal.CockpitCamera), "ClampRotationY", new[] { typeof(Quaternion).MakeByRefType(), typeof(float) });
				_harmony.Patch(clampRotationY, new HarmonyMethod(typeof(CockpitFreeLook), nameof(WidenClamp)));

				var calcAngles = AccessTools.Method(typeof(CarX.Internal.CockpitCamera), "CalculateEstimatedAnglesByView", new[] { typeof(Vector3) });
				_harmony.Patch(calcAngles, postfix: new HarmonyMethod(typeof(CockpitFreeLook), nameof(WidenAngles)));
			}
			catch (Exception)
			{
			}
		}

		void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}

		private static void AddPitch(ref Quaternion rotation)
		{
			var input = GameInput.InputManager.instance;
			var prefs = DI.DependencyInjector.Resolve<GamePrefs>();
			Vector2 stick = Vector2.zero;
			bool joystickActive = input != null && prefs != null &&
				input.IsLastActiveJoystickAxis2D(prefs.globals.cameraOrbitStick, out stick) && stick != Vector2.zero;
			bool mouseActive = !joystickActive && input != null && input.GetActionValue(GameInput.ActionType.FreeCamView) > 0f;

			float targetPitch;
			float lerpCoef;
			if (joystickActive)
			{
				targetPitch = Mathf.Clamp(stick.y, -1f, 1f) * MaxPitch;
				lerpCoef = JoystickLerpCoef;
			}
			else if (mouseActive)
			{
				_mouseCachedPitch = Mathf.Clamp(_mouseCachedPitch + ReInput.controllers.Mouse.GetAxis(1) * MousePitchScale, -MaxPitch, MaxPitch);
				targetPitch = _mouseCachedPitch;
				lerpCoef = MouseLerpCoef;
			}
			else
			{
				targetPitch = 0f;
				lerpCoef = JoystickLerpCoef;
			}

			_currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * lerpCoef);
			rotation *= Quaternion.Euler(-_currentPitch, 0f, 0f);
		}

		private static void WidenClamp(ref float maxRotAngle)
		{
			if (maxRotAngle > 0f)
			{
				maxRotAngle += YawBonus;
			}
		}

		private static void WidenAngles(CarX.Internal.CockpitCamera __instance)
		{
			LeftAngleRef(__instance) -= YawBonus;
			RightAngleRef(__instance) += YawBonus;
		}
	}
}
