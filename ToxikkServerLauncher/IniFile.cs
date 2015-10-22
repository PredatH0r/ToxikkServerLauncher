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
      private readonly Dictionary<string, string> data = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
      
      public Section(string name)
      {
        this.Name = name;
      }

      #region Name
      public string Name { get; private set; }
      #endregion

      #region Set()
      internal void Set(string key, string value)
      {
        data[key] = value;
      }
      #endregion

      #region Keys
      public IEnumerable<string> Keys => data.Keys;

      #endregion

      #region GetString()

      public string GetString(string key)
      {
        string value;
        if (!data.TryGetValue(key, out value))
          return null;
        return value;
      }

      #endregion

      #region GetInt()

      public int GetInt(string key, int defaultValue = 0)
      {
        string value;
        if (!data.TryGetValue(key, out value))
          return defaultValue;
        return this.ParseNumber(value);
      }

      #endregion

      #region GetBytes()
      public byte[] GetBytes(string key)
      {
        string value;
        if (!data.TryGetValue(key, out value))
          return null;
        if (string.IsNullOrEmpty(value))
          return new byte[0];

        string[] parts = value.Split(',');
        byte[] bytes = new byte[parts.Length];
        int i = 0;
        foreach (var part in parts)
          bytes[i++] = (byte)this.ParseNumber(part);
        return bytes;
      }

      #endregion

      #region GetBool()
      public bool GetBool(string setting, bool defaultValue = false)
      {
        var val = this.GetString(setting);
        if (val == null) return defaultValue;
        val = val.ToLower();
        return val == "1" || val == "true" || val == "yes" || val == "on";
      }
      #endregion

      #region GetDecimal()
      public decimal GetDecimal(string key)
      {
        string value = this.GetString(key);
        if (value == null)
          return 0;
        decimal val;
        decimal.TryParse(value, out val);
        return val;
      }
      #endregion

      #region GetIntList()
      public int[] GetIntList(string key)
      {
        string value = this.GetString(key);
        if (string.IsNullOrEmpty(value))
          return new int[0];
        string[] numbers = value.Split(',');
        int[] ret = new int[numbers.Length];
        for (int i = 0; i < numbers.Length; i++)
          ret[i] = this.ParseNumber(numbers[i]);
        return ret;
      }
      #endregion

      #region ParseNumber()
      private int ParseNumber(string value)
      {
        if (value.ToLower().StartsWith("0x"))
        {
          try { return Convert.ToInt32(value, 16); }
          catch { return 0; }
        }
        int intValue;
        int.TryParse(value, out intValue);
        return intValue;
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
            if (idx < 0)
              continue;
            key = trimmedLine.Substring(0, idx).Trim();
            val = "";
          }
                      
          if (line.EndsWith("\\"))
            val += line.Substring(idx + 1, line.Length - idx - 1 - 1).Trim() + "\n";
          else
          {
            val += line.Substring(idx + 1).Trim();
            currentSection.Set(key, val);
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
          sb.AppendLine($"{key}={section.GetString(key)}");
        sb.AppendLine();
      }
      File.WriteAllText(this.fileName, sb.ToString());
    }
    #endregion
  }
}
