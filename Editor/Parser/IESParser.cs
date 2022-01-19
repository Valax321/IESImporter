using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Valax321.IESImporter
{
    internal class IESParser
    {
        /**
         * Using https://knowledge.autodesk.com/support/autocad/learn-explore/caas/CloudHelp/cloudhelp/2019/ENU/AutoCAD-Core/files/GUID-45CAAF1C-7C9D-49A7-B18D-00CA5E2ED380-htm.html
         * for the IES file spec. It's the 1991 version instead of 1995 but they seem almost the same.
         */

        // How many different versions are out there??
        public enum IESVersion
        {
            IESNA91,
            IESNA95,
        }
        
        public static IESParser Read(StreamReader reader)
        {
            return new IESParser(reader);
        }
        
        public float ratedLumens { get; private set; }
        public float candelaMultiplier { get; private set; }
        public int verticalAngleCount { get; private set; }
        public int horizontalAngleCount { get; private set; }
        public bool usingMetres { get; private set; }
        
        public float luminousOpeningWidth { get; private set; }
        public float luminousOpeningLength { get; private set; }
        public float luminousOpeningHeight { get; private set; }
        
        public float lightWatts { get; private set; }
        public float maxIntensity { get; private set; }

        public SampleData[,] samples { get; private set; }

        private IESVersion m_Version;
        
        private IESParser(StreamReader reader)
        {
            ParseHeader(reader);
        }

        private void ParseHeader(StreamReader reader)
        {
            var version = GetNextToken(reader);
            m_Version = version switch
            {
                "IESNA91" => IESVersion.IESNA91,
                "IESNA:LM-63-1995" => IESVersion.IESNA95,
                _ => throw new Exception($"Unknown IES file version {version}")
            };
            
            while (!reader.EndOfStream)
            {
                var token = GetNextToken(reader);
                if (!string.IsNullOrEmpty(token) && char.IsDigit(token[0]))
                {
                    ParseData(reader, token);
                    break;
                }
                
                reader.ReadLine(); // Skip the rest of the line
            }
        }

        private void ParseData(StreamReader reader, string initialToken)
        {
            ratedLumens = float.Parse(GetNextToken(reader));
            candelaMultiplier = float.Parse(GetNextToken(reader));
            verticalAngleCount = int.Parse(GetNextToken(reader));
            horizontalAngleCount = int.Parse(GetNextToken(reader));

            samples = new SampleData[horizontalAngleCount, verticalAngleCount];
            
            // Useless 1's
            _ = GetNextToken(reader);

            usingMetres = int.Parse(GetNextToken(reader)) == 2;
            luminousOpeningWidth = float.Parse(GetNextToken(reader));
            luminousOpeningLength = float.Parse(GetNextToken(reader));
            luminousOpeningHeight = float.Parse(GetNextToken(reader));

            // Useless 1's
            _ = GetNextToken(reader);
            _ = GetNextToken(reader);
            
            // Not sure about this: the 1991 spec says this will be 0, but in my test files it seems to
            // match the reported wattage of the lamp. We don't use it either way so it should
            // be ok.
            lightWatts = float.Parse(GetNextToken(reader));

            // Ok now the angle data
            var verticalAngles = new List<float>(verticalAngleCount);
            for (var y = 0; y < verticalAngleCount; y++)
            {
                verticalAngles.Add(float.Parse(GetNextToken(reader)));
            }
            for (var h = 0; h < horizontalAngleCount; h++)
            {
                var hAngle = float.Parse(GetNextToken(reader));
                
                // We can now fill the sample data angles
                for (var v = 0; v < verticalAngleCount; v++)
                {
                    var sampleData = new SampleData { HorizontalAngle = hAngle, VerticalAngle = verticalAngles[v] };
                    samples[h, v] = sampleData;
                }
            }

            // And now the intensity data
            for (var h = 0; h < horizontalAngleCount; h++)
            {
                for (var v = 0; v < verticalAngleCount; v++)
                {
                    var sample = samples[h, v];
                    sample.Intensity = float.Parse(GetNextToken(reader)) * candelaMultiplier;
                    if (sample.Intensity > maxIntensity)
                        maxIntensity = sample.Intensity;

                    samples[h, v] = sample;
                }
            }
        }

        private static string GetNextToken(StreamReader reader)
        {
            var sb = new StringBuilder();
            
            int chr;
            while ((chr = reader.Read()) != -1)
            {
                var character = (char)chr;

                if (char.IsWhiteSpace(character))
                {
                    // If it's whitespace, but this token is empty,
                    // just skip the remaining whitespace until we hit something meaningful
                    if (sb.Length > 0)
                        break;
                }
                else sb.Append(character);
            }

            return sb.ToString();
        }
    }
}