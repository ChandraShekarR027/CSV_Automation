using System.Collections.Generic;
using System.IO;
using System;

class Program
{
    static void Main()
    {
        string filePath = "C:\\Users\\GRL\\Downloads\\vol1.csv"; // CSV file path
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

        // Step 1: Remove rows that contain "Reserved" and delete the next row if it is "DELIMITER"
        for (int i = 0; i < csvData.Count; i++)
        {
            if (csvData[i][0].Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the "Reserved" row
                csvData.RemoveAt(i);

                // Check if the next row is "DELIMITER" and remove it as well
                if (i < csvData.Count && csvData[i][0].Equals("DILIMTER", StringComparison.OrdinalIgnoreCase))
                {
                    csvData.RemoveAt(i);
                }

                // Adjust the index after removal
                i--;
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
                while (j < csvData.Count && csvData[j][0] != "DELIMTER" && csvData[j][0] != "CHANNEL_NO")
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
                for (int k = i + 2; k < csvData.Count && csvData[k][0] != "BLOCK_ID"; k++)
                {
                    blockLengthSum += int.Parse(csvData[k][2]);
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

