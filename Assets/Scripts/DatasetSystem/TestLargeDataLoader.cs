using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class CsvToBinaryConverter : MonoBehaviour
{
    //[Tooltip("Path to your huge CSV (e.g. Assets/Data/huge_data.csv)")]
    private string csvPath = "C:\\Users\\epuls\\Downloads\\barcode_protein_sequences_square_002um.csv";
    //[Tooltip("Where to write the .bin (e.g. Library/huge_data.bin)")]
    private string binPath = "C:\\Users\\epuls\\Downloads\\barcode_protein_sequences_square_002um.bin";
    [Tooltip("Lines per frame before yielding")]
    public int batchLines = 10_000;

    void Start()
    {
        /*
        StartCoroutine(ConvertCsvToBinary(() => {
            Debug.Log("✅ CSV → binary conversion complete!");
        }));
        */
    }

    private IEnumerator ConvertCsvToBinary(Action onComplete)
    {
        long written = 0;
        using var reader = new StreamReader(csvPath, Encoding.ASCII, false, 1 << 20);
        using var writer = new BinaryWriter(File.Open(binPath, FileMode.Create), Encoding.ASCII);

        string line;
        int counter = 0;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(',');
            if (parts.Length == 3)
            {
                // encode each column into ASCII bytes
                byte[] bBar = Encoding.ASCII.GetBytes(parts[0] ?? "");
                byte[] bNuc = Encoding.ASCII.GetBytes(parts[1] ?? "");
                byte[] bProt = Encoding.ASCII.GetBytes(parts[2] ?? "");

                // write lengths + data
                writer.Write(bBar.Length);
                writer.Write(bNuc.Length);
                writer.Write(bProt.Length);
                writer.Write(bBar);
                writer.Write(bNuc);
                writer.Write(bProt);

                written++;
            }

            if ((++counter) % batchLines == 0)
                yield return null;
        }

        Debug.Log($"→ Wrote {written:n0} records to {binPath}");
        onComplete?.Invoke();
    }
}