using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CSV_Automation;

class Program
{
    static void Main()
    {
        string inputFolderPath = Path.Combine(Environment.CurrentDirectory, "Input\\"); // Input folder path
        string sourceFilePath = @"D:\Git-grl01\CSV_Automation\CSV_Automation\bin\Debug\Rev_3Referance\Rev3(1).csv"; // Reference

        Decoder d1 = new Decoder();

        try
        {
            // Get all CSV files from the input folder
            string[] csvFiles = Directory.GetFiles(inputFolderPath, "*.csv");

            foreach (string filePath in csvFiles)
            {
                Console.WriteLine($"Processing file: {filePath}");

                // Reinitialize List to store CSV data for each file
                List<string[]> csvData = new List<string[]>();

                try
                {
                    // Reading each CSV file
                    using (var reader = new StreamReader(filePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (line != null)
                            {
                                var values = line.Split(',');
                                csvData.Add(values);
                            }
                        }
                    }

                    // Step 1: Remove all END_FRAME rows and Cable 13(Block 13) in void.csv
                    d1.RemoveEndFrameRows(csvData);

                    // Step 2: Remove rows that contain "Reserved" and handle DELIMITER
                    d1.RemoveReserved(csvData);

                    // Step 3: Update No_Of_Points rows for both ADC and normal blocks
                    d1.UpdateNoOfPoints(csvData);

                    // Step 4: Calculate BLOCK_LENGTH and update offsets
                    d1.UpdateBlockLengthAndOffsets(csvData);

                    // Step 5: Copy specific data based on conditions from sourceFilePath to csvData
                    d1.CompareAndCopyData(sourceFilePath, csvData);

                    // Step 6: Save the Output sheet as 2 different CSV and .bin files in a new folder
                    d1.SaveAsCsvAndBin(ref inputFolderPath, csvData);

                    Console.WriteLine("CSV file updated successfully.");
                    Thread.Sleep(5000);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"I/O error while processing file {filePath}: {ioEx.Message}");
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Console.WriteLine($"Access error while processing file {filePath}: {uaEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while processing file {filePath}: {ex.Message}");
                }

                // Clear csvData for the next file
                csvData.Clear();
            }
        }
        catch (DirectoryNotFoundException dnEx)
        {
            Console.WriteLine($"Input folder not found: {dnEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in Main method: {ex.Message}");
        }
    }
}
