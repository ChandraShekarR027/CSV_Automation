using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main()
    {
        string filePath = "C:\\Users\\GRL\\Downloads\\0559.csv";//epath
        string sourceFilePath = "C:\\Users\\GRL\\Downloads\\04003.csv"; // Source file path
        List<string[]> csvData = new List<string[]>();

        // Reading the CSV file
        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                csvData.Add(values);
            }
        }
       // Remove block_13(Cable_IR)

        // Step 1: Remove all END_FRAME rows in void.csv
        RemoveEndFrameRows(csvData);

        // Step 2: Remove rows that contain "Reserved" and handle DELIMITER
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

                if (inBlockIdRange)
                {
                    // Check if the next DELIMITER exists
                    for (int j = i + 1; j < csvData.Count; j++)
                    {
                        if (csvData[j][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                        {
                            inBlockIdRange = true;
                            break;
                        }
                        else if (csvData[j][0].Equals("BLOCK_ID", StringComparison.OrdinalIgnoreCase))
                        {
                            inBlockIdRange = false;
                            break;
                        }
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

        // Step 3: Update No_Of_Points rows
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0] == "CHANNEL_NO")//FOR ADC BLOCK
            {
                int pairCount = 0;
                int j = i + 1;

                // Process rows until DELIMITER or another CHANNEL_NO is found
                while (j < csvData.Count && csvData[j][0] != "DILMITER" && csvData[j][0] != "CHANNEL_NO")
                {
                    // Check for pairs of VOLTAGE/CURRENT and ADC_COUNT rows
                    if ((csvData[j][0].StartsWith("VOLTAGE") || csvData[j][0].StartsWith("CURRENT"))
                        && j + 1 < csvData.Count && csvData[j + 1][0] == "ADC_COUNT")
                    {
                        pairCount++;
                        j++; // Skip the ADC_COUNT row
                    }
                    j++; // Move to the next row
                }

                // Insert the No_Of_Points row after the CHANNEL_NO
                string[] noOfPointsRow = new string[] { "No_Of_Points", "0", "1", pairCount.ToString() };
                csvData.Insert(i + 1, noOfPointsRow);
                i++; // Skip the inserted No_Of_Points row

                // Update the OFFSET values after adding No_Of_Points
                UpdateOffsets(csvData, i);
            }
            // Step 4.1: Calculate sum and insert No_Of_Points row after BLOCK_ID with 4th column value 14 FOR NORMAL BLOCK
            if (csvData[i][0] == "BLOCK_ID" && csvData[i].Length > 3 && (csvData[i][3] == "14" || csvData[i][3] == "12" || csvData[i][3] == "11"))
            {
                int calculatedSum = 0;
                int offset = int.Parse(csvData[i][1]); // Assuming the offset is in the second column
                int j = i + 1;

                // Sum values in the fourth column until BLOCK_START is encountered
                while (j < csvData.Count && csvData[j][0] != "BLOCK_START")
                {
                    if (int.TryParse(csvData[j][2], out int value))
                    {
                        calculatedSum += value;
                    }
                    j++;
                }

                // Insert the No_Of_Points row after the BLOCK_ID row
                string[] noOfPointsRow = new string[] { "No_Of_Points", offset.ToString(), "1", calculatedSum.ToString() };
                csvData.Insert(i + 1, noOfPointsRow);
                i++; // Skip the inserted No_Of_Points row
            }
        }

        // Step 4: Calculate BLOCK_LENGTH and update offsets
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0] == "BLOCK_ID")
            {
                // Calculate OFFSET for BLOCK_LENGTH row
                int offset = int.Parse(csvData[i][1]) + int.Parse(csvData[i][2]);

                // Insert the BLOCK_LENGTH row after the BLOCK_ID row
                string[] blockLengthRow = new string[] { "BLOCK_LENGTH", offset.ToString(), "2", "0" }; // Placeholder "0" for the 4th column
                csvData.Insert(i + 1, blockLengthRow);

                // Calculate the BLOCK LENGTH value
                int blockLengthSum = 0;


                for (int k = i + 1; k < csvData.Count && csvData[k][0] != "BLOCK_START" && csvData[k][0].Trim() != "END_FRAM"; k++)
                {
                    // Include the length of rows that are considered part of the block, including No_Of_Points
                    if (csvData[k][0] == "DILIMTER")
                    {

                        // Include the all DELIMITER in the calculation
                        blockLengthSum += int.Parse(csvData[k][2]);


                    }
                    else
                    {
                        blockLengthSum += int.Parse(csvData[k][2]);
                    }
                }

                // Set the calculated sum to the BLOCK_LENGTH row
                csvData[i + 1][3] = blockLengthSum.ToString();

                // Skip the inserted BLOCK_LENGTH row in the loop
                i++;

                // Update the OFFSET values after adding BLOCK_LENGTH
                UpdateOffsets(csvData, i);
            }
        }


        // Step 5: Copy specific data based on conditions from sourceFilePath to csvData
        CompareAndCopyData(sourceFilePath, csvData);
        SaveAsRev3AndUpdateFramRev(ref filePath, csvData);
        string newFilePath = GetNewFilePath(filePath);//for creating new files

        // Step 6: Write the updated data back to the CSV file
        using (var writer = new StreamWriter(newFilePath))
        {
            foreach (var row in csvData)
            {
                writer.WriteLine(string.Join(",", row));
            }
        }
        
        Console.WriteLine("CSV file updated successfully.");
    }
    
   

    static void RemoveEndFrameRows(List<string[]> csvData)
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




    static void CompareAndCopyData(string sourceFilePath, List<string[]> csvData)
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
            UpdateOffsets(csvData, 0);

            Console.WriteLine("Missing data from filePath2 has been copied to csvData.");
        }
        else
        {
            Console.WriteLine("Data counts are the same. No additional data to copy.");
        }
        
    }

    // Helper method to read a CSV file into a list of string arrays
    static List<string[]> ReadCsv(string sourceFilePath)
    {
        var data = new List<string[]>();
        using (var reader = new StreamReader(sourceFilePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                data.Add(values);
            }
        }
        return data;
    }


    static void UpdateOffsets(List<string[]> csvData, int startIndex)
    {
        for (int i = startIndex; i < csvData.Count; i++)
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
                }
            }
        }
    }
    static void SaveAsRev3AndUpdateFramRev(ref string filePath, List<string[]> csvData)
    {
        // Update the FRAM_REV row by setting the 3rd column to "3"
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0].Equals("FRAM_REV", StringComparison.OrdinalIgnoreCase))
            {
                csvData[i][3] = "3"; // Set the 3rd column to "3"
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
                    Console.WriteLine(sumLastRow);
                    Console.ReadLine();
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

        // Save the modified csvData to the new file
        string newFilePath = GetNewFilePath(filePath);
        using (StreamWriter writer = new StreamWriter(newFilePath))
        {
            foreach (var row in csvData)
            {
                writer.WriteLine(string.Join(",", row));
            }
        }

        Console.WriteLine($"Original file {filePath} updated with FRAM_REV and LENGTH information, and saved as {newFilePath}.");
    }

    static string GetNewFilePath(string originalFilePath)
    {
        // Define the new directory path
        string baseDirectory = @"C:\GRL\GRL_Mpp_Calibration\Rev3 Automated sheets";

        // Create the new directory if it does not exist
        if (!Directory.Exists(baseDirectory))
        {
            Directory.CreateDirectory(baseDirectory);
        }

        // Extract the file name and extension from the original file path
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
        string extension = Path.GetExtension(originalFilePath);

        // Generate a timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Create the new file name with timestamp
        string newFileName = $"{fileNameWithoutExtension}_Rev3_{timestamp}{extension}";

        // Combine the new directory path with the new file name
        return Path.Combine(baseDirectory, newFileName);
    }





}

