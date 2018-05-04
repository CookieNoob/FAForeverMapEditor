﻿// ******************************************************************************
//
// * Tables.lua Class - Adaptive maps code made by CookieNoob
// * Can be loaded from LUA and saved as LUA using LuaParser
// * Parsing is done by hand, because I can't find good parser that will convert LUA to Class
// * Copyright ozonexo3 2017
//
// ******************************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLua;

namespace MapLua
{
	[System.Serializable]
	public class TablesLua
	{
		public bool IsLoaded;
		public TablesInfo Data = new TablesInfo();
		Lua LuaFile;


		#region Structure Objects
		[System.Serializable]
		public class TablesInfo
		{
			// Core Marker to Army
			public MexArray[] spawnMexArmy = new MexArray[5];
			public MexArray[] spawnHydroArmy = new MexArray[5];

			public List<TableKey> AllTables;
		}



		[System.Serializable]
		public struct MexArray
		{
			public string[] MexesIds;

			public MexArray(string[] MexesIds)
			{
				this.MexesIds = MexesIds;
			}
		}

		[System.Serializable]
		public struct TableKey
		{
			public string Key;
			public bool OneDimension;
			public MexArray[] Values;
			public bool IsHydro;

			public TableKey(string Key, bool OneDimension)
			{
				this.Key = Key;
				this.OneDimension = OneDimension;
				IsHydro = Key.ToLower().Contains("hydro");

				if (OneDimension)
				{
					Values = new MexArray[1];
					Values[0] = new MexArray(new string[0]);
				}
				else
				{
					Values = new MexArray[0];
				}
			}

		}
		#endregion


		const string KEY_spwnMexArmy = "spwnMexArmy";
		const string KEY_spwnHydroArmy = "spwnHydroArmy";

		public bool Load(string FolderName, string ScenarioFileName, string FolderParentPath)
		{
			IsLoaded = false;
			System.Text.Encoding encodeType = System.Text.Encoding.ASCII;

			//string MapPath = EnvPaths.GetMapsPath();

			string loadedFile = "";
			string loc = FolderParentPath + FolderName + "/" + ScenarioFileName + ".lua";
			loc = loc.Replace("_scenario.lua", "_tables.lua");

			Debug.Log("Load file:" + loc);

			if (!System.IO.File.Exists(loc))
			{
				Debug.Log("No Tables file found");
				return false;
			}

			loadedFile = System.IO.File.ReadAllText(loc, encodeType);// .Replace("}\n", "},\n").Replace("} ", "}, ");

			LuaFile = new Lua();
			LuaFile.LoadCLRPackage();
			try
			{
				LuaFile.DoString(MapLuaParser.Current.SaveLuaHeader.text + loadedFile);
			}
			catch (NLua.Exceptions.LuaException e)
			{
				Debug.LogError(LuaParser.Read.FormatException(e), MapLuaParser.Current.gameObject);
				return false;
			}

			string[] Keys = GetAllTableKeys(loadedFile);
			Data.AllTables = new List<TableKey>();

			GetMexArrays(LuaFile.GetTable(KEY_spwnMexArmy), ref Data.spawnMexArmy);
			GetMexArrays(LuaFile.GetTable(KEY_spwnHydroArmy), ref Data.spawnHydroArmy);

			//Debug.Log(Keys.Length);
			for(int i = 0; i < Keys.Length; i++)
			{
				LuaTable MarkerTable = LuaFile.GetTable(Keys[i]);
				if (MarkerTable != null)
				{

					//Debug.Log(Keys[i] + ": " + MarkerTable.Values.Count);

					string[] StringValues = LuaParser.Read.StringArrayFromTable(MarkerTable);

					if(StringValues.Length == 0 || StringValues[0] != "table")
					{
						TableKey NewTable = new TableKey(Keys[i], true);
						NewTable.Values[0].MexesIds = StringValues;

						Data.AllTables.Add(NewTable);
					}
					else
					{
						TableKey NewTable = new TableKey(Keys[i], false);
						GetMexArrays(MarkerTable, ref NewTable.Values);
						Data.AllTables.Add(NewTable);
					}

				}
			}

			SaveLua.Scenario SaveLuaData = MapLuaParser.Current.SaveLuaFile.Data;
			for (int mc = 0; mc < SaveLuaData.MasterChains.Length; mc++)
			{
				for(int m = 0; m < SaveLuaData.MasterChains[mc].Markers.Count; m++)
				{
					if(SaveLuaData.MasterChains[mc].Markers[m].MarkerType == SaveLua.Marker.MarkerTypes.Mass)
					{ // Mex
						if (!SaveLuaData.MasterChains[mc].Markers[m].Name.ToLower().StartsWith("mass "))
							continue;

						string NameKey = SaveLuaData.MasterChains[mc].Markers[m].Name.Remove(0, 5);

						SaveLuaData.MasterChains[mc].Markers[m].SpawnWithArmy = new List<int>();
						SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys = new List<string>();

						for (int i = 0; i < Data.spawnMexArmy.Length; i++)
						{
							for(int k = 0; k < Data.spawnMexArmy[i].MexesIds.Length; k++)
							{
								if(Data.spawnMexArmy[i].MexesIds[k] == NameKey){
									SaveLuaData.MasterChains[mc].Markers[m].SpawnWithArmy.Add(i);
								}
							}
						}

						for(int i = 0; i < Data.AllTables.Count; i++)
						{
							if (!Data.AllTables[i].IsHydro)
							{
								if (Data.AllTables[i].OneDimension)
								{
									for(int k = 0; k < Data.AllTables[i].Values[0].MexesIds.Length; k++)
									{
										if (Data.AllTables[i].Values[0].MexesIds[k] == NameKey)
										{
											SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys.Add(Data.AllTables[i].Key);
										}
									}
								}
								else
								{
									for (int t = 0; t < Data.AllTables[i].Values.Length; t++)
									{
										for (int k = 0; k < Data.AllTables[i].Values[t].MexesIds.Length; k++)
										{
											if (Data.AllTables[i].Values[t].MexesIds[k] == NameKey)
											{
												SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys.Add(Data.AllTables[i].Key + "#" + t);
											}
										}
									}
								}
							}
						}
					}
					else if (SaveLuaData.MasterChains[mc].Markers[m].MarkerType == SaveLua.Marker.MarkerTypes.Hydrocarbon)
					{ // Hydro
						if (!SaveLuaData.MasterChains[mc].Markers[m].Name.ToLower().StartsWith("hydrocarbon "))
							continue;

						string NameKey = SaveLuaData.MasterChains[mc].Markers[m].Name.Remove(0, 12);

						SaveLuaData.MasterChains[mc].Markers[m].SpawnWithArmy = new List<int>();
						SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys = new List<string>();

						for (int i = 0; i < Data.spawnHydroArmy.Length; i++)
						{
							for (int k = 0; k < Data.spawnHydroArmy[i].MexesIds.Length; k++)
							{
								if (Data.spawnHydroArmy[i].MexesIds[k] == NameKey)
								{
									SaveLuaData.MasterChains[mc].Markers[m].SpawnWithArmy.Add(i);
								}
							}
						}

						for (int i = 0; i < Data.AllTables.Count; i++)
						{
							if (Data.AllTables[i].IsHydro)
							{
								if (Data.AllTables[i].OneDimension)
								{
									for (int k = 0; k < Data.AllTables[i].Values[0].MexesIds.Length; k++)
									{
										if (Data.AllTables[i].Values[0].MexesIds[k] == NameKey)
										{
											SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys.Add(Data.AllTables[i].Key);
										}
									}
								}
								else
								{
									for (int t = 0; t < Data.AllTables[i].Values.Length; t++)
									{
										for (int k = 0; k < Data.AllTables[i].Values[t].MexesIds.Length; k++)
										{
											if (Data.AllTables[i].Values[t].MexesIds[k] == NameKey)
											{
												SaveLuaData.MasterChains[mc].Markers[m].AdaptiveKeys.Add(Data.AllTables[i].Key + "#" + t);
											}
										}
									}
								}
							}
						}

					}
				}
			}


			IsLoaded = true;
			return true;
		}

		void GetMexArrays(LuaTable Table, ref MexArray[] Array)
		{
			LuaTable[] Tabs = LuaParser.Read.TableArrayFromTable(Table);
			Array = new MexArray[Tabs.Length];
			for (int i = 0; i < Array.Length; i++)
				Array[i] = new MexArray(LuaParser.Read.StringArrayFromTable(Tabs[i]));
		}

		string[] GetAllTableKeys(string file)
		{
			List<string> Keys = new List<string>();
			file = file.Replace(" ", "");
			string[] Lines = file.Split("\n".ToCharArray());
			for(int l = 0; l < Lines.Length; l++)
			{
				if (Lines[l].Contains("="))
				{
					string value = Lines[l].Split("=".ToCharArray())[0];
					if (value == KEY_spwnMexArmy || value == KEY_spwnHydroArmy)
						continue;
					Keys.Add(value);
				}
			}


			return Keys.ToArray();
		}
	}
}