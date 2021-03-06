﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Selection
{
	public partial class SelectionManager : MonoBehaviour
	{

		public static SelectionManager Current;

		[Header("Core")]
		public Camera Cam;
		public RectTransform SelBox;
		public int DisableLayer;
		public int UsedLayer;
		public bool SnapToGrid;
		public bool SnapToWater = true;

		public GameObject[] AffectedGameObjects;
		public int[] AffectedTypes;
		public bool Active = false;


		void Awake()
		{
			Current = this;
		}

		private void OnEnable()
		{
			SymmetryWindow.OnSymmetryChanged += OnSymmetryChanged;
		}

		private void OnDisable()
		{
			SymmetryWindow.OnSymmetryChanged -= OnSymmetryChanged;

		}

		void Update()
		{
			if (!Active)
				return;

			if (AllowRemove && Input.GetKeyDown(KeyCode.Delete))
			{
				DestroySelectedObjects();
			}

		}


		public enum SelectionControlTypes
		{
			None, Last, Marker, MarkerChain, Decal, Units
		}

		SelectionControlTypes LastControlType;
		void SetSelectionType(SelectionControlTypes SelectionControlType)
		{
			if (LastControlType == SelectionControlType)
				return;

			FinishSelectionChange();

			switch (SelectionControlType)
			{
				case SelectionControlTypes.Marker:
					AllowMove = true;
					AllowUp = false;
					AllowRotation = true;
					AllowRotationX = false;
					AllowLocalRotation = false;
					AllowScale = false;
					AllowCustomScale = false;
					AllowSnapToGrid = true;
					AllowSelection = true;
					AllowSymmetry = true;
					AllowRemove = true;
					SelPrefab = 0;
					MinAngle = 90;
					break;
				case SelectionControlTypes.MarkerChain:
					AllowMove = false;
					AllowUp = false;
					AllowRotation = true;
					AllowRotationX = false;
					AllowLocalRotation = false;
					AllowScale = false;
					AllowCustomScale = false;
					AllowSnapToGrid = true;
					AllowSelection = true;
					AllowSymmetry = false;
					AllowRemove = false;
					SelPrefab = 0;
					MinAngle = 90;
					break;
				case SelectionControlTypes.Decal:
					AllowMove = true;
					AllowUp = false;
					AllowRotation = true;
					AllowRotationX = true;
					AllowLocalRotation = true;
					AllowScale = true;
					AllowCustomScale = true;
					AllowSnapToGrid = false;
					AllowSelection = true;
					AllowSymmetry = true;
					AllowRemove = true;
					SelPrefab = 1;
					MinAngle = 0;
					break;
				case SelectionControlTypes.Units:
					AllowMove = true;
					AllowUp = false;
					AllowRotation = true;
					AllowRotationX = false;
					AllowLocalRotation = true;
					AllowScale = false;
					AllowCustomScale = false;
					AllowSnapToGrid = true;
					AllowSelection = true;
					AllowSymmetry = true;
					AllowRemove = true;
					SelPrefab = 2;
					MinAngle = 90;
					break;
				default:
					AllowMove = false;
					AllowUp = false;
					AllowRotation = false;
					AllowRotationX = false;
					AllowLocalRotation = false;
					AllowScale = false;
					AllowCustomScale = false;
					AllowSnapToGrid = true;
					AllowSelection = false;
					AllowSymmetry = false;
					AllowRemove = false;
					SelPrefab = 0;
					MinAngle = 0;
					break;
			}
			LastControlType = SelectionControlType;

		}

		public static bool AllowSelection = true;
		public static bool AllowSymmetry = true;
		public static bool AllowRemove = true;
		public static bool AllowSnapToGrid = true;
		public static bool AllowMove;
		public static bool AllowUp;
		public static bool AllowRotation;
		public static bool AllowRotationX;
		public static bool AllowLocalRotation;
		public static bool AllowScale;
		public static bool AllowCustomScale;
		int SelPrefab = 0;


		public void ClearAffectedGameObjects(bool ResetTools = true)
		{
			Undo.Current.RegisterSelectionRangeChange();
			if(ResetTools)
				SetAffectedGameObjects(new GameObject[0], SelectionControlTypes.None);
			else
				SetAffectedGameObjects(new GameObject[0], SelectionControlTypes.Last);

			AffectedTypes = new int[0];
		}


		/*
		public void SetCustomSettings(bool _Selection = true, bool _Symmetry = true, bool _Remove = true)
		{
			if (AllowSymmetry != _Selection)
				FinishSelectionChange();
			else if(AllowSymmetry != _Symmetry)
				FinishSelectionChange();
			else if (AllowRemove != _Remove)
				FinishSelectionChange();

			AllowSelection = _Selection;
			AllowSymmetry = _Symmetry;
			AllowRemove = _Remove;
		}
		*/

		public void SetAffectedGameObjects(GameObject[] GameObjects, SelectionControlTypes SelectionControlType)
		{
			if(SelectionControlType != SelectionControlTypes.Last)
				SetSelectionType(SelectionControlType);

			ChangeControlerType.UpdateCurrent();

			if (AffectedGameObjects.Length > 0)
			{
				// Clean
				for(int i = 0; i < AffectedGameObjects.Length; i++)
				{
					if(AffectedGameObjects[i])
						AffectedGameObjects[i].layer = DisableLayer;
				}
			}

			AffectedGameObjects = GameObjects;

			if (AffectedGameObjects.Length > 0)
			{
				// activate settings
				for (int i = 0; i < AffectedGameObjects.Length; i++)
				{
					if (AffectedGameObjects[i])
						AffectedGameObjects[i].layer = UsedLayer;
				}
			}


			Active = AffectedGameObjects.Length > 0;

			CleanIfInactive();
		}

		public void SetAffectedTypes(int[] NewTypes)
		{
			AffectedTypes = NewTypes;
		}


		public void CleanSelection()
		{
			Selection.Ids = new List<int>();
			FinishSelectionChange();
		}

		void CleanIfInactive()
		{
			if (!Active)
			{
				AffectedGameObjects = new GameObject[0];
				AffectedTypes = new int[0];
				CleanSelection();
			}
		}


		#region Action Events
		static System.Action<List<GameObject>, bool> RemoveAction;
		public void SetRemoveAction(System.Action<List<GameObject>, bool> Action)
		{
			RemoveAction = Action;
		}


		void DestroySelectedObjects()
		{
			int Count = Selection.Ids.Count;
			if (Count > 0)
			{
				List<GameObject> SelectedObjectsList = new List<GameObject>();
				for(int i = 0; i < Count; i++)
				{
					SelectedObjectsList.Add(AffectedGameObjects[Selection.Ids[i]]);
				}


				for(int s = 0; s < SymetrySelection.Length; s++)
				{
					Count = SymetrySelection[s].Ids.Count;

					for (int i = 0; i < Count; i++)
					{
						SelectedObjectsList.Add(AffectedGameObjects[SymetrySelection[s].Ids[i]]);
					}
				}

				RemoveAction(SelectedObjectsList, true);

				Active = AffectedGameObjects.Length > 0;

				CleanIfInactive();
			}
		}

		static System.Action SelectionChangeAction;
		public void SetSelectionChangeAction(System.Action Action)
		{
			SelectionChangeAction = Action;
		}

		static System.Action<Transform, GameObject> CustomSnapAction;
		public void SetCustomSnapAction(System.Action<Transform, GameObject> Action)
		{
			CustomSnapAction = Action;
		}

		#endregion
	}
}
