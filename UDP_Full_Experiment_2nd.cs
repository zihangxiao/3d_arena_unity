using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDP_Full_Experiment_2nd : MonoBehaviour
{
    public Transform playerCamera;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private int port = 33333;
    private float targetHeadingDegrees = 0.0f;
    private float baselineSpeed = 0.5f; // Baseline constant speed for movement
    private float currentSpeed; // Speed that can be adjusted by data
    private float currentHeadingVelocity; // For smooth rotation
    private List<string> positionLog = new List<string>();
    private string baseDirectory = @"C:\Users\xiaozihang\Documents\unity_de";

    void Start()
    {
        currentSpeed = 0; // Initialize with baseline speed
        remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        udpClient = new UdpClient();
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        udpClient.Client.ReceiveTimeout = 1000;
        BeginReceive();
        StartCoroutine(SampleData());
        StartCoroutine(CaptureScreenshots());
        Invoke("EndExperiment", 5 * 60);
    }

    private void BeginReceive()
    {
        udpClient.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Byte[] receivedBytes = udpClient.EndReceive(ar, ref remoteEndPoint);
            string receivedData = Encoding.UTF8.GetString(receivedBytes);
            ProcessReceivedData(receivedData);
        }
        catch (Exception e)
        {
            Debug.LogError("Receive Failed:" + e.ToString());
        }
        BeginReceive(); // Resume receiving data
    }


    private IEnumerator CaptureScreenshots()
    {
        int screenshotCount = 0;
        int totalScreenshots = (int)(5 * 60 / 0.2);
        string folderPath = Path.Combine(baseDirectory, "Screenshots_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        while (screenshotCount < totalScreenshots)
        {
            string filename = $"screenshot_{screenshotCount}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}.png";
            string filePath = Path.Combine(folderPath, filename);

            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log("Saved screenshot to " + filePath);

            screenshotCount++;
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator SampleData()
    {
        while (true)
        {
            string positionData = $"{Time.time:F2},{playerCamera.position.x:F2},{playerCamera.position.y:F2},{playerCamera.eulerAngles.y:F2}";
            positionLog.Add(positionData);
            yield return new WaitForSeconds(0.02f);
        }
    }

    private void ProcessReceivedData(string data)
    {
        string[] tokens = data.Split(',');

        if (tokens.Length < 24 || tokens[0].Trim() != "FT")
        {
            Debug.Log("Bad read");
            return;
        }

        try
        {
            float headingRadians = float.Parse(tokens[17], CultureInfo.InvariantCulture);
            targetHeadingDegrees = headingRadians * Mathf.Rad2Deg;
            float speed = float.Parse(tokens[19], CultureInfo.InvariantCulture);
            currentSpeed = speed+baselineSpeed; // Update speed based on received data
        }
        catch (FormatException e)
        {
            Debug.LogError("Error parsing data: " + e.Message);
        }
    }

    void Update()
    {
        if (playerCamera != null)
        {
            float currentHeadingDegrees = playerCamera.eulerAngles.y;
            float smoothTime = 0.3f; // Adjust smooth time as needed
            float newHeading = Mathf.SmoothDampAngle(currentHeadingDegrees, targetHeadingDegrees, ref currentHeadingVelocity, smoothTime);
            playerCamera.rotation = Quaternion.Euler(0, newHeading, 0);

            playerCamera.position += playerCamera.forward * currentSpeed * Time.deltaTime;
        }
    }

    private void SaveToCSV()
    {
        string directory = baseDirectory; // Use the base directory
        string filename = $"FruitFlyData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(directory, filename);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Time,X,Y,Heading");
            foreach (string line in positionLog)
            {
                writer.WriteLine(line);
            }
        }
        Debug.Log($"Data saved to {filePath}");
    }

    private void EndExperiment()
    {
        StopAllCoroutines();
        SaveToCSV();
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient.Dispose();
        }
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        EndExperiment();
    }
}


