using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SoundRecordManager : MonoBehaviour
{
    public enum RecordState { RECORDING, STOPPED }
    private RecordState currentState;

    [SerializeField] private Button startRecordingButton;
    [SerializeField] private Button stopRecordingButton;
    [SerializeField] private Button discardRecordingButton;
    [SerializeField] private Button saveRecordingButton;
    [SerializeField] private int recordingLength;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private string recordingDirName = "micRecordings";
    [SerializeField] private string soundFilenameRegistry = "myRecordings.txt";
    [SerializeField] private CanvasGroup canvas;

    private string micID;
    private AudioClip currentMicClip;
    private string registryPath;

    void Start()
    {
        ChangeRecordingState(1);
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("There's no mic connected!");
            Application.Quit();
        }

        micID = Microphone.devices[0];
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, recordingDirName));

        registryPath = Path.Combine(Application.persistentDataPath, soundFilenameRegistry);

        // how to read/write to text: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.appendtext
        if (!File.Exists(registryPath)) File.CreateText(registryPath);
        else
        {
            StartCoroutine(GetRecordedClips());
        }
    }

    private void SetCurrentState(RecordState value)
    {
        if (currentState != value)
        {
            if (value == RecordState.RECORDING)
            {
                startRecordingButton.gameObject.SetActive(false);
                stopRecordingButton.gameObject.SetActive(true);
                discardRecordingButton.interactable = false;
                saveRecordingButton.interactable = false;

                currentMicClip = Microphone.Start(micID, false, recordingLength, 44100);
            }
            else if (value == RecordState.STOPPED)
            {
                startRecordingButton.gameObject.SetActive(true);
                stopRecordingButton.gameObject.SetActive(false);

                if (currentMicClip != null)
                {
                    discardRecordingButton.interactable = true;
                    saveRecordingButton.interactable = true;
                }
                else
                {
                    discardRecordingButton.interactable = false;
                    saveRecordingButton.interactable = false;
                }


                if (micID != null) Microphone.End(micID);
            }
            currentState = value;
        }
    }

    IEnumerator GetRecordedClips()
    {
        canvas.interactable = false;
        using StreamReader sr = File.OpenText(registryPath);
        string s = "";
        while ((s = sr.ReadLine()) != null)
        {
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Path.Combine(Application.persistentDataPath, recordingDirName, s), AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log(www.error);
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                SpawnSoundObject(myClip);
            }
        }
        canvas.interactable = true;
    }

    public void ChangeRecordingState(int stateIndex)
    {
        SetCurrentState((RecordState)stateIndex);
    }

    public void DiscardRecording()
    {
        currentMicClip = null;
    }

    private void SpawnSoundObject(AudioClip clip)
    {
        Vector3 newPosition = new(UnityEngine.Random.Range(-5, 5), 0, UnityEngine.Random.Range(-5, 5));
        GameObject spawn = Instantiate(spawnPrefab, newPosition, Quaternion.identity);
        spawn.GetComponent<AudioSource>().clip = clip;
    }

    public void SaveRecording()
    {
        string filename = Time.time.ToString();
        SavWav.Save(Path.Combine(recordingDirName, filename), currentMicClip);

        using (StreamWriter sw = File.AppendText(registryPath))
        {
            sw.WriteLine(filename + ".wav");
        }

        SpawnSoundObject(currentMicClip);

        currentMicClip = null;
        discardRecordingButton.enabled = false;
        saveRecordingButton.enabled = false;
    }
}
