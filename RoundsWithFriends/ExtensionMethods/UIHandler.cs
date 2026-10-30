using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;

namespace RWF
{
	public class UIHandlerAdditionalData
	{
		public bool disableTexts = false;
	}

	public static class UIHandlerExtensions
	{
		private static readonly ConditionalWeakTable<UIHandler, UIHandlerAdditionalData> additionalData = new ConditionalWeakTable<UIHandler, UIHandlerAdditionalData>();

		public static UIHandlerAdditionalData GetData(this UIHandler instance)
		{
			return additionalData.GetOrCreateValue(instance);
		}

		public static bool ShouldShowLobbyRoundCounter()
		{
			if (GameModeManager.CurrentHandler == null)
			{
				return false;
			}

			if (GameModeManager.CurrentHandlerID == GameModeManager.SandBoxID)
			{
				return false;
			}

			return GameModeManager.CurrentHandler.Settings != null
				&& GameModeManager.CurrentHandler.Settings.ContainsKey(RWFMod.RoundsToWinGameKey);
		}

		public static void SyncLobbyRoundCounter(this UIHandler instance)
		{
			if (instance == null)
			{
				return;
			}

			if (!UIHandlerExtensions.ShouldShowLobbyRoundCounter())
			{
				instance.roundCounterSmall.gameObject.SetActive(false);
				instance.InvokeMethod("HideRoundCounterSmall");
				return;
			}

			int rounds = RWFMod.instance != null ? RWFMod.instance.RoundsToWinGame : 5;
			if (GameModeManager.CurrentHandler?.Settings != null
				&& GameModeManager.CurrentHandler.Settings.TryGetValue(RWFMod.RoundsToWinGameKey, out object value)
				&& value is int settingRounds)
			{
				rounds = settingRounds;
			}

			instance.InvokeMethod("SetNumberOfRounds", rounds);

			Dictionary<int, int> teamPoints = new Dictionary<int, int>();
			Dictionary<int, int> teamRounds = new Dictionary<int, int>();

			if (PlayerManager.instance != null && PlayerManager.instance.players != null && PlayerManager.instance.players.Any())
			{
				foreach (Player player in PlayerManager.instance.players)
				{
					if (!teamPoints.ContainsKey(player.teamID))
					{
						teamPoints[player.teamID] = 0;
						teamRounds[player.teamID] = 0;
					}
				}
			}
			else
			{
				teamPoints[0] = 0;
				teamPoints[1] = 0;
				teamRounds[0] = 0;
				teamRounds[1] = 0;
			}

			instance.ShowRoundCounterSmall(teamPoints, teamRounds);
		}

		// Overload for the existing ShowRoundCounterSmall method to support more than two teams
		public static void ShowRoundCounterSmall(this UIHandler instance, Dictionary<int, int> teamPoints, Dictionary<int, int> teamRounds) {
			instance.roundCounterSmall.gameObject.SetActive(true);
			instance.roundCounterSmall.UpdateRounds(teamRounds);
			instance.roundCounterSmall.UpdatePoints(teamPoints);
			if (instance.roundCounterAnimSmall.currentState != CodeAnimationInstance.AnimationUse.In) {
				instance.roundCounterAnimSmall.PlayIn();
			}
		}

		public static void DisplayRoundStartText(this UIHandler instance, string text) {
			var uiGo = GameObject.Find("/Game/UI");
			var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
			var roundStartTextGo = gameGo.transform.Find("RoundStartText");

			var roundStartTextPart = roundStartTextGo.GetComponentInChildren<GeneralParticleSystem>();
			var roundStartText = roundStartTextGo.GetComponent<TextMeshProUGUI>();
			var roundStartPulse = roundStartTextGo.GetComponent<UI.ScalePulse>();

			roundStartTextPart.particleSettings.color = Color.white;
			roundStartTextPart.duration = 60f;
			roundStartTextPart.loop = true;
			roundStartTextPart.Play();
			roundStartText.text = text;
			instance.StopAllCoroutines();
			instance.StartCoroutine(roundStartPulse.StartPulse());
		}

		public static void HideRoundStartText(this UIHandler instance) {
			var uiGo = GameObject.Find("/Game/UI");
			var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
			var roundStartTextGo = gameGo.transform.Find("RoundStartText");

			var roundStartTextPart = roundStartTextGo.GetComponentInChildren<GeneralParticleSystem>();
			var roundStartPulse = roundStartTextGo.GetComponent<UI.ScalePulse>();

			roundStartTextPart.loop = false;
		}

		public static void DisableTexts(this UIHandler instance, float duration)
        {
			instance.GetData().disableTexts = true;
			instance.ExecuteAfterSeconds(duration, () => instance.GetData().disableTexts = false);
        }
	}
}
