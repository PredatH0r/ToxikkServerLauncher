﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ToxikkServerLauncher
{
  public class IniFile
  {
    #region class Section

    public class Section
    {
      #region class IniValue
      public class IniValue
      {
        public string Value;
        public string Operator;

        public IniValue(string val, string op = "=")
        {
          Value = val;
          Operator = op;
        }
      }
      #endregion
      private readonly Dictionary<string, List<IniValue>> data = new Dictionary<string, List<IniValue>>(StringComparer.CurrentCultureIgnoreCase);

      public Section(string name)
      {
        this.Name = name;
      }

      #region Name
      public string Name { get; private set; }
      #endregion

      #region Add()
      internal void Add(string key, string value, string op = "=")
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list))
        {
          list = new List<IniValue>();
          data.Add(key, list);
        }
        list.Add(new IniValue(value, op));
      }
      #endregion

      #region Remove()
      internal void Remove(string key, string value)
      {
        List<IniValue> list;
        if (data.TryGetValue(key, out list))
        {
          for (int i = 0; i < list.Count; i++)
          {
            if (list[i].Value == value)
            {
              list.RemoveAt(i);
              --i;
            }
          }
        }          
      }
      #endregion

      #region Set()
      internal void Set(string key, string value, string op = "=")
      {
        data[key] = new List<IniValue> { new IniValue(value, op) };
      }
      #endregion

      #region Keys
      public IEnumerable<string> Keys => data.Keys;

      #endregion

      #region GetString()
      public string GetString(string key)
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list))
          return null;
        return list[0].Value;
      }
      #endregion

      #region GetBool()
      public bool GetBool(string key, bool defaultValue = false)
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list) || list.Count == 0)
          return defaultValue;
        var val = list[0].Value.ToLower();
        if (val == "")
          return defaultValue;
        return val != "0" && val != "false";
      }
      #endregion

      #region GetInt()
      public int GetInt(string key, int defaultValue = 0)
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list) || list.Count == 0)
          return defaultValue;
        var val = list[0].Value.ToLower();
        if (val == "")
          return defaultValue;
        int intVal;
        return int.TryParse(val, out intVal) ? intVal : defaultValue;
      }
      #endregion

      #region GetDecimal()
      public decimal GetDecimal(string key, decimal defaultValue = 0)
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list) || list.Count == 0)
          return defaultValue;
        var val = list[0].Value.ToLower();
        if (val == "")
          return defaultValue;
        decimal intVal;
        return decimal.TryParse(val, out intVal) ? intVal : defaultValue;
      }
      #endregion


      #region GetAll()

      public List<IniValue> GetAll(string key)
      {
        List<IniValue> list;
        if (!data.TryGetValue(key, out list))
          return new List<IniValue>();
        return list;
      }

      #endregion

    }
    #endregion

    private readonly Dictionary<string, Section> sectionDict;
    private readonly List<Section> sectionList;
    private readonly string fileName;

    public IniFile(string fileName)
    {
      this.sectionDict = new Dictionary<string, Section>();
      this.sectionList = new List<Section>();
      this.fileName = fileName;
      this.ReadIniFile();
    }

    public IEnumerable<Section> Sections => this.sectionList;
    public string FileName => this.fileName;

    public Section GetSection(string sectionName, bool create = false)
    {
      Section section;
      sectionDict.TryGetValue(sectionName, out section);
      if (section == null)
      {
        section = new Section(sectionName);
        sectionList.Add(section);
        sectionDict.Add(sectionName, section);
      }
      return section;
    }

    #region ReadIniFile()
    private void ReadIniFile()
    {
      if (!File.Exists(fileName))
        return;
      using (StreamReader rdr = new StreamReader(fileName))
      {
        Section currentSection = null;
        string line;
        string key = null;
        string val = null;
        string op = null;
        while ((line = rdr.ReadLine()) != null)
        {
          string trimmedLine = line.Trim();
          if (trimmedLine.StartsWith(";"))
            continue;
          if (trimmedLine.StartsWith("["))
          {
            string sectionName = trimmedLine.EndsWith("]")
                                   ? trimmedLine.Substring(1, trimmedLine.Length - 2)
                                   : trimmedLine.Substring(1);
            currentSection = new Section(sectionName);
            this.sectionList.Add(currentSection);
            this.sectionDict[sectionName] = currentSection;
            continue;
          }
          if (currentSection == null)
            continue;

          int idx = -1;
          if (val == null)
          {
            idx = trimmedLine.IndexOf("=");
            if (idx <= 0)
              continue;

            if (trimmedLine[idx - 1] == '+' || trimmedLine[idx - 1] == '-')
            {
              op = trimmedLine[idx - 1] + "=";
              key = trimmedLine.Substring(0, idx - 1).Trim();
            }
            else
            {
              op = "=";
              key = trimmedLine.Substring(0, idx).Trim();
            }
            val = "";
          }

          if (line.EndsWith("\\"))
            val += line.Substring(idx + 1, line.Length - idx - 1 - 1).Trim() + "\n";
          else
          {
            val += line.Substring(idx + 1).Trim();
            currentSection.Add(key, val, op);
            val = null;
          }
        }
      }
    }
    #endregion

    #region Save()
    public void Save()
    {
      var sb = new StringBuilder();
      foreach (var section in this.sectionList)
      {
        sb.Append("[").Append(section.Name).AppendLine("]");
        foreach (var key in section.Keys)
        {
          foreach (var value in section.GetAll(key))
            sb.AppendLine($"{key}{value.Operator}{value.Value}");
        }
        sb.AppendLine();
      }
      File.WriteAllText(this.fileName, sb.ToString());
    }
    #endregion
  }
}
