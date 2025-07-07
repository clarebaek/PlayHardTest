using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// 베이스코드출처 : https://chopchops.tistory.com/11
public class ExcelParser
{
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    public static Dictionary<string, Dictionary<string, object>> Read(string file)
    {
        var list = new Dictionary<string, Dictionary<string, object>>();
        TextAsset data = Resources.Load(file) as TextAsset;

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        var typename = Regex.Split(lines[0], SPLIT_RE);

        var header = Regex.Split(lines[1], SPLIT_RE);

        for (var i = 2; i < lines.Length; i++)
        {

            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");

                value = value.Replace("<br>", "\n"); // 추가된 부분. 개행문자를 \n대신 <br>로 사용한다.
                value = value.Replace("<c>", ",");

                object finalvalue = value;

                switch (typename[j])
                {
                    case "string":
                        finalvalue = value;
                        break;
                    case "uint":
                        uint itemp;
                        if (uint.TryParse(value, out itemp))
                        {
                            finalvalue = itemp;
                        }
                        break;
                    case "long":
                        long ltemp;
                        if (long.TryParse(value, out ltemp))
                        {
                            finalvalue = ltemp;
                        }
                        break;
                    case "#":
                        // 그냥 주석용 컬럼임으로 아무런 처리도 하ㅣ않음.
                        continue;
                }

                entry[header[j]] = finalvalue;
            }

            if (list.ContainsKey(values[0]) == true)
            {
                Debug.Log("ERROR!!!!!");
                continue;
            }
            else
            {
                list.Add(values[0], entry);
            }
        }
        return list;
    }
}