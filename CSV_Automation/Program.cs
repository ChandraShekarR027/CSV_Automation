using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main()
    {
        string filePath = "C:\\Users\\GRL\\Downloads\\void.csv"; // CSV file path
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

        // Step 1: Remove rows that contain "Reserved" and handle DELIMITER
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0].Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;

                // Check if the row before the Reserved has DELIMITER
                if (i > 0 && csvData[i - 1][0].Equals("DELIMITER", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }

                // Check if the row after the Reserved has DELIMITER
                if (i + 1 < csvData.Count && csvData[i + 1][0].Equals("DELIMITER", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }

                // If count is 2 or more, delete the DELIMITER below the Reserved
                if (count >= 2)
                {
                    if (i + 1 < csvData.Count && csvData[i + 1][0].Equals("DELIMITER", StringComparison.OrdinalIgnoreCase))
                    {
                        csvData.RemoveAt(i + 1);
                    }
                }

                // Remove the Reserved row itself
                csvData.RemoveAt(i);
                i--; // Adjust index after removal
            }
        }

        // Step 2: Update No_Of_Points rows
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0] == "CHANNEL_NO")
            {
                int pairCount = 0;
                int j = i + 1;

                // Process rows until DELIMITER or another CHANNEL_NO is found
                while (j < csvData.Count && csvData[j][0] != "DELIMITER" && csvData[j][0] != "CHANNEL_NO")
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
        }

        // Step 3: Calculate BLOCK_LENGTH and update offsets
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
                bool delimiterEncountered = false;

                for (int k = i + 2; k < csvData.Count && csvData[k][0] != "BLOCK_ID"; k++)
                {
                    if (csvData[k][0] == "DELIMITER")
                    {
                        if (!delimiterEncountered)
                        {
                            // Include the first DELIMITER in the calculation
                            blockLengthSum += int.Parse(csvData[k][2]);
                            delimiterEncountered = true;
                        }
                    }
                    else
                    {
                        blockLengthSum += int.Parse(csvData[k][2]);
                    }
                }
                csvData[i + 1][3] = blockLengthSum.ToString();

                // Skip the inserted BLOCK_LENGTH row in the loop
                i++;

                // Update the OFFSET values after adding BLOCK_LENGTH
                UpdateOffsets(csvData, i);
            }
        }

        // Write the updated data back to the CSV file
        using (var writer = new StreamWriter(filePath))
        {
            foreach (var row in csvData)
            {
                writer.WriteLine(string.Join(",", row));
            }
        }

        Console.WriteLine("CSV file updated successfully.");
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
}
