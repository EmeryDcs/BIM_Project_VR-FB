using UnityEngine;
using System.IO;

public class DataFeedbacks : MonoBehaviour
{
	public static DataFeedbacks Instance { get; private set; }

	[SerializeField]
	private string folderName = "FeedbacksData";
	private string filePath = "";

	private int idPlayer = 0; // Vous pouvez personnaliser cela selon vos besoins
	private StreamWriter writer;

	private void Start()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogWarning("Multiple instances of DataFeedbacks detected. Destroying the new one.");
			Destroy(this.gameObject);
			return;
		} 
		Instance = this;
	}

	public void InitialiseFile()
	{
		// Combine correctement les chemins
		string folderPath = Path.Combine(Application.persistentDataPath, folderName);

		// Crée le dossier si nécessaire
		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}

		// Crée le nom du fichier au format date_heure + type de tracker
		string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		string fileName = $"{timestamp}_FeedbackData_{idPlayer}.csv";

		// Chemin complet du fichier
		filePath = Path.Combine(folderPath, fileName);

		// Initialise le StreamWriter
		writer = new StreamWriter(filePath, false);  // false écrase le fichier existant
		writer.WriteLine("Time; " +
			"Feedback Type ; " + //0: Individuel Gaze, 1: Individuel Speech, 2: Individuel Face, 3: Collectif Gaze, 4: Collectif Speech, 5: Collectif Face
			"Description Event"); //0: Start, 1: Stop
		writer.WriteLine();
		Debug.Log("Recording started: " + filePath);
	}

	public void AddFeedbackLog(int feedbackType, string description)
	{
		if (writer != null)
		{
			writer.WriteLine($"{Time.time}; {feedbackType} ; {description}");
			writer.Flush(); // Ensure data is written to file immediately
		}
		else
		{
			Debug.LogError($"Writer is null.");
		}
	}

	public void StopRecording()
	{
		if (writer != null)
		{
			writer.Write($"END");
			writer.Flush(); // Ensure all data is written to disk
			writer.Close();
			writer.Dispose(); // Ensure file is properly closed
			writer = null;
		}
		Debug.Log("Recording stopped.");
	}


	private void OnDestroy()
	{
		Debug.Log("Application quittée sur OnDestroy. Arrêt de l'enregistrement des Feedbacks.");
		StopRecording();
	}

	private void OnApplicationQuit()
	{
		Debug.Log("Application quittée. Arrêt de l'enregistrement des Feedbacks.");
		StopRecording();
	}

	public void SetIdPlayer(int id)
	{
		idPlayer = id;
	}
}
