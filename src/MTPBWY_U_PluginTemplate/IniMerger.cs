using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTPBWY_U_Palworld;

internal class IniMerger
{
    public static void MergeInis(string tempIni, string targetIniContent, string targetFilePath, string sectionName)
    {
        List<string> tempIniLines = tempIni.Split(Environment.NewLine).ToList();
        File.WriteAllLines(targetFilePath, tempIniLines);
    }
}