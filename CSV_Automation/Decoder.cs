using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSV_Automation
{
     class Decoder
    {
        public void RemoveEndFrameRows(List<string[]> csvData)
        {
            for (int i = csvData.Count - 1; i >= 0; i--)
            {
                // Check if the current row is "END_FRAM" and remove it
                if (csvData[i][0].Trim() == "END_FRAM")
                {
                    csvData.RemoveAt(i);
                }
                // Check for BLOCK_ID with the fourth column value of 13
                else if (csvData[i][0].Equals("BLOCK_ID", StringComparison.OrdinalIgnoreCase) && csvData[i].Length > 3 && csvData[i][3].Trim() == "13")
                {
                    // Always remove the row immediately before BLOCK_ID
                    if (i - 1 >= 0)
                    {
                        csvData.RemoveAt(i - 1);
                        i--; // Adjust index since the row above BLOCK_ID was removed
                    }

                    // Continue removing rows after BLOCK_ID until a BLOCK_START is found
                    while (i < csvData.Count && !csvData[i][0].Equals("BLOCK_START", StringComparison.OrdinalIgnoreCase))
                    {
                        csvData.RemoveAt(i);
                    }
                }
            }
        }




        public void CompareAndCopyData(string sourceFilePath, List<string[]> csvData)
        {
            // Calculate the count of data in csvData
            int countFile1 = csvData.Count;

            // Read data from filePath2
            List<string[]> dataFile2 = ReadCsv(sourceFilePath);
            int countFile2 = dataFile2.Count;

            // Check if counts are different
            if (countFile1 != countFile2)
            {
                // Calculate where to start copying from filePath2
                int startIndex = countFile1;

                List<string[]> copiedData = new List<string[]>();

                // Copy data from filePath2 starting from startIndex
                for (int i = startIndex; i < dataFile2.Count; i++)
                {
                    copiedData.Add(dataFile2[i]);
                }

                // Append the copied data to csvData
                csvData.AddRange(copiedData);

                // Update offsets after adding the data
                UpdateOffsets(csvData);

                Console.WriteLine("Missing data from filePath2 has been copied to csvData.");
                Thread.Sleep(5000);
            }
            else
            {
                Console.WriteLine("Data counts are the same. No additional data to copy.");
                Thread.Sleep(5000);
            }

        }

        // Helper method to read a CSV file into a list of string arrays
        public List<string[]> ReadCsv(string sourceFilePath)
        {
            var data = new List<string[]>();
            try
            {
                // Read the entire file content into a string
                var fileContent = File.ReadAllText(sourceFilePath);
                var lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Process each line
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = line.Split(',');
                        data.Add(values);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Access to the file was denied: " + ex.Message);
            }
            return data;
        }



        public void UpdateOffsets(List<string[]> csvData)
        {
            for (int i = 2; i < csvData.Count; i++)
            {
                if (i > 0)
                {
                    if (int.TryParse(csvData[i - 1][1], out int previousOffset) && int.TryParse(csvData[i - 1][2], out int previousSize))
                    {
                        csvData[i][1] = (previousOffset + previousSize).ToString();
                    }
                    else
                    {
                        Console.WriteLine($"Invalid numeric format at row {i}. Skipping offset update.");
                        Thread.Sleep(5000);
                    }
                }
            }
        }
        public void SaveAsCsvAndBin(ref string filePath, List<string[]> csvData)
        {
            // Update FRAM_REV and LENGTH in the csvData
            SaveAsRev3AndUpdateFramRev(ref filePath, csvData);

            string systemSerialNum = ExtractSystemSerialNum(csvData);
            string framRev = ExtractFramRev(csvData);

            if (systemSerialNum == null || framRev == null)
            {
                Console.WriteLine("Unable to extract system serial number or FRAM_REV from the CSV data.");
                return;
            }

            // Define the output directory
            string outputDirectory = @"D:\Git-grl01\CSV_Automation\CSV_Automation\bin\Debug\Output";

            // Ensure the output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Generate filenames based on extracted values
            string csvFilePath = Path.Combine(outputDirectory, $"{"C3TPR"}_{systemSerialNum+"BB"}_{"Rev3"}.csv");
            string binFilePath = Path.Combine(outputDirectory, $"{"C3TPR"}_{systemSerialNum+"BB"}_{"Rev3"}.bin");
            
            // Save data as CSV file
            using (StreamWriter writer = new StreamWriter(csvFilePath))
            {
                foreach (var row in csvData)
                {
                    writer.WriteLine(string.Join(",", row));
                }
            }

            // Save data as BIN file (binary format)
            using (FileStream fs = new FileStream(binFilePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter binWriter = new BinaryWriter(fs))
            {
                foreach (var row in csvData)
                {
                    foreach (var value in row)
                    {
                        binWriter.Write(value); // Write each value as a binary string
                    }
                }
            }

            Console.WriteLine($"CSV file saved as: {csvFilePath}");
            Console.WriteLine($"BIN file saved as: {binFilePath}");
        }
        private string ExtractSystemSerialNum(List<string[]> csvData)
        {
            // Implement logic to extract the system serial number
            // For example, searching for a specific row or column where the serial number is located
            // Placeholder implementation
            foreach (var row in csvData)
            {
                if (row.Length > 0 && row[0].StartsWith("SYSTEM_SERIAL_NUMBER"))
                {
                    return row[3]; // Assume the serial number is in the second column
                }
            }
            return null;
        }

        public  string ExtractFramRev(List<string[]> csvData)
        {
            // Implement logic to extract the FRAM_REV value
            // For example, searching for a specific row or column where FRAM_REV is located
            // Placeholder implementation
            foreach (var row in csvData)
            {
                if (row.Length > 2 && row[0] == "FRAM_REV")
                {
                    return row[3]; // Assume the FRAM_REV value is in the third column
                }
            }
            return null;
        }





        public void SaveAsRev3AndUpdateFramRev(ref string filePath, List<string[]> csvData)
        {
            for (int i = 0; i < csvData.Count; i++)
            {
                if (csvData[i][0].Equals("FRAM_REV", StringComparison.OrdinalIgnoreCase))
                {
                    csvData[i][3] = "3";
                }
                if (csvData[i][0].Equals("LICENSE", StringComparison.OrdinalIgnoreCase))
                {
                    csvData[i][3] = "65535";
                    csvData[i+1][3] = "65535";
                    break;
                }

            }
            // Calculate the sum of the 1st and 2nd columns of the last row
            int sumLastRow = 0;
            if (csvData.Count > 0)
            {
                var lastRow = csvData[csvData.Count - 1];
                if (lastRow.Length > 2)
                {
                    int firstValue, secondValue;
                    if (int.TryParse(lastRow[1], out firstValue) && int.TryParse(lastRow[2], out secondValue))
                    {
                        sumLastRow = firstValue + secondValue;

                    }
                }
            }

            // Find and update the LENGTH row's 3rd column
            for (int i = 0; i < csvData.Count; i++)
            {
                if (csvData[i][0].Equals("LENGTH", StringComparison.OrdinalIgnoreCase))
                {
                    if (csvData[i].Length > 3)
                    {
                        csvData[i][3] = sumLastRow.ToString(); // Update the 3rd column of LENGTH row
                    }
                    break;
                }
            }


        }
        public void RemoveReserved(List<string[]> csvData)
        {
            for (int i = 0; i < csvData.Count; i++)
            {
                if (csvData[i][0].Equals("Reserved", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if Reserved is between BLOCK_ID (with 3rd column 0) and DELIMITER
                    bool inBlockIdRange = false;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (csvData[j][0].Equals("BLOCK_ID", StringComparison.OrdinalIgnoreCase) && csvData[j][3].Trim() == "0")
                        {
                            inBlockIdRange = true;
                            break;
                        }
                        else if (csvData[j][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                        {
                            inBlockIdRange = false;
                            break;
                        }
                    }

                    if (!inBlockIdRange)
                    {
                        int count = 0;

                        // Check if the row before the Reserved has DELIMITER
                        if (i > 0 && csvData[i - 1][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                        }

                        // Check if the row after the Reserved has DELIMITER
                        if (i + 1 < csvData.Count && csvData[i + 1][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                        }

                        // If count is 2 or more, delete the DELIMITER below the Reserved
                        if (count >= 2)
                        {
                            if (i + 1 < csvData.Count && csvData[i + 1][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                            {
                                csvData.RemoveAt(i + 1);
                            }
                        }

                        // Remove the Reserved row itself
                        csvData.RemoveAt(i);
                        i--; // Adjust index after removal
                    }
                }
            }
        }
        public void UpdateBlockLengthAndOffsets(List<string[]> csvData)
        {
            for (int i = 0; i < csvData.Count; i++)
            {
                if (csvData[i][0] == "BLOCK_ID")
                {
                    int offset = int.Parse(csvData[i][1]) + int.Parse(csvData[i][2]);
                    string[] blockLengthRow = new string[] { "BLOCK_LENGTH", offset.ToString(), "2", "0", "IS INCLUDING BLOCK LENGTH AND INCLUDING DELIMETER", "0" };
                    csvData.Insert(i + 1, blockLengthRow);

                    int blockLengthSum = 0;
                    for (int k = i + 1; k < csvData.Count && csvData[k][0] != "BLOCK_START" && csvData[k][0].Trim() != "END_FRAM"; k++)
                    {
                        blockLengthSum += int.Parse(csvData[k][2]);
                    }

                    csvData[i + 1][3] = blockLengthSum.ToString();
                    i++;

                   
                }
            }
            UpdateOffsets(csvData);
        }


        public void UpdateNoOfPoints(List<string[]> csvData)
        {
            for (int i = 0; i < csvData.Count; i++)
            {
                if (csvData[i][0] == "CHANNEL_NO")
                {
                    int pairCount = 0;
                    int j = i + 1;

                    while (j < csvData.Count && csvData[j][0] != "DELIMITER" && csvData[j][0] != "CHANNEL_NO")
                    {
                        if ((csvData[j][0].StartsWith("VOLTAGE") || csvData[j][0].StartsWith("CURRENT") || csvData[j][0].StartsWith("TEMPERATURE")) && j + 1 < csvData.Count && csvData[j + 1][0] == "ADC_COUNT")
                        {
                            pairCount++;
                            j++;
                        }
                        j++;
                    }

                    string[] noOfPointsRow = new string[] { "No_Of_Points", "0", "1", pairCount.ToString(), "Number of point","0" };
                    csvData.Insert(i + 1, noOfPointsRow);
                    i++;

                    UpdateOffsets(csvData);
                }

                if (csvData[i][0] == "BLOCK_ID" && (csvData[i][3] == "14" || csvData[i][3] == "12" || csvData[i][3] == "11"))
                {
                    int calculatedSum = 0;
                    int offset = int.Parse(csvData[i][1]);
                    int j = i + 1;

                    while (j < csvData.Count && csvData[j][0] != "BLOCK_START")
                    {
                        if (int.TryParse(csvData[j][2], out int value))
                        {
                            calculatedSum += value;
                        }
                        j++;
                    }

                    string[] noOfPointsRow = new string[] { "No_Of_Points", offset.ToString(), "1", calculatedSum.ToString(), "Number of point", "0" };
                    csvData.Insert(i + 1, noOfPointsRow);
                    i++;
                }
            }
        }
    }
}
