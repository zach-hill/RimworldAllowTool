﻿using System;
using AllowTool.Context;
using AllowTool.Settings;
using HugsLib;
using HugsLib.Utils;
using Verse;

namespace AllowTool {
	/// <summary>
	/// The hub of the mod. 
	/// </summary>
	[EarlyInit]
	public class AllowToolController : ModBase {
		public static AllowToolController Instance { get; private set; }

		public override string ModIdentifier {
			get { return "AllowTool"; }
		}

		// needed to access protected field from static getter below
		private ModLogger GetLogger {
			get { return base.Logger; }
		}
		internal new static ModLogger Logger {
			get { return Instance.GetLogger; }
		}

		public UnlimitedDesignationDragger Dragger { get; private set; }
		public WorldSettings WorldSettings { get; private set; }
		public ModSettingsHandler Handles { get; private set; }
		public ReflectionHandler Reflection { get; private set; }
		private HotKeyHandler hotKeys;
		private bool dependencyRefreshScheduled;

		private AllowToolController() {
			Instance = this;
		}

		public override void EarlyInitalize() {
			Dragger = new UnlimitedDesignationDragger();
			Handles = new ModSettingsHandler();
			Reflection = new ReflectionHandler();
			Reflection.PrepareReflection();
			hotKeys = new HotKeyHandler();
			Compat_PickUpAndHaul.Apply();
		}

		public override void Update() {
			Dragger.Update();
			DesignatorContextMenuController.Update();
		}

		public override void Tick(int currentTick) {
			DesignationCleanupManager.Tick(currentTick);
		}

		public override void OnGUI() {
			hotKeys.OnGUI();
		}

		public override void WorldLoaded() {
			WorldSettings = UtilityWorldObjectManager.GetUtilityWorldObject<WorldSettings>();
		}

		public override void MapLoaded(Map map) {
			// necessary when adding the mod to existing saves
			var injected = AllowToolUtility.EnsureAllColonistsKnowAllWorkTypes(map);
			if (injected) {
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.HaulingUrgent, map);
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.FinishingOff, map);
			}
		}

		public override void SettingsChanged() {
			ResolveAllDesignationCategories();
			if (AllowToolUtility.ReverseDesignatorDatabaseInitialized) {
				Find.ReverseDesignatorDatabase.Reinit();
			}
		}

		internal void OnBeforeImpliedDefGeneration() {
			try {
				// setting handles bust be created after language data is loaded
				// and before DesignationCategoryDef.ResolveDesignators is called
				// implied def generation is a good loading stage to do that on
				Handles.PrepareSettingsHandles(Instance.Settings);

				if (!Handles.HaulWorktypeSetting) {
					AllowToolDefOf.HaulingUrgent.visible = false;
				}
				if (Handles.FinishOffWorktypeSetting) {
					AllowToolDefOf.FinishingOff.visible = true;
				}
			} catch (Exception e) {
				Logger.Error("Error during early setting handle setup: "+e);
			}
		}

		internal void OnDesignationCategoryResolveDesignators() {
			ScheduleDesignatorDependencyRefresh();
		}

		internal void OnReverseDesignatorDatabaseInit(ReverseDesignatorDatabase database) {
			ReverseDesignatorProvider.InjectReverseDesignators(database);
			ScheduleDesignatorDependencyRefresh();
		}

		internal void ScheduleDesignatorDependencyRefresh() {
			if (dependencyRefreshScheduled) return;
			dependencyRefreshScheduled = true;
			// push the job to the next frame to avoid repeating this for every category as the game loads
			HugsLibController.Instance.DoLater.DoNextUpdate(() => {
				try {
					dependencyRefreshScheduled = false;
					hotKeys.RebindAllDesignators();
					DesignatorContextMenuController.RebindAllContextMenus();
				} catch (Exception e) {
					Logger.Error($"Error during designator dependency refresh: {e}");
				}
			});
		}

		private void ResolveAllDesignationCategories() {
			foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefs) {
				Reflection.DesignationCategoryDefResolveDesignatorsMethod.Invoke(categoryDef, new object[0]);
			}
		}
	}
}