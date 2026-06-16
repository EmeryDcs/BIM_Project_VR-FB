using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeakingInterruption : NetworkBehaviour
{
	public static SpeakingInterruption Instance { get; private set; }

	[Tooltip("L'index de l'orateur principal (celui à qui on peut couper la parole). -1 = personne.")]
	[SerializeField]
	private int currentSpeaker = -1;

	[Tooltip("Le temps (en secondes) pendant lequel le joueur peut faire une pause sans perdre la parole.")]
	[SerializeField]
	private float noPauseTiming = 0.5f;

	[SerializeField]
	[Networked]
	private NetworkObject text { get; set; }
	[Networked, Capacity(128)]
	private string stringToDisplay { get; set; } = "";
	[SerializeField]
	private AudioSource announcementFeedback;

	[Tooltip("Booléen pour chaque prise de parole")]
	[Networked]
	private bool isLecteurSpeaking { get; set; }
	[Networked]
	private bool isCalculateurSpeaking { get; set; }
	[Networked]
	private bool isModelisateurSpeaking { get; set; }
	private bool[] isSpeakingThisFrame = new bool[3] {false, false, false};
	[Networked]
	private bool isShowingFeedback { get; set; } = false;

	[Networked]
	private int hasJustInterrupted { get; set; } = -1;

	// Permet de tracker le temps de silence de chaque joueur pour tolérer les pauses
	private float[] silenceTimers = new float[] { 999f, 999f, 999f }; // Initialisé grand pour éviter un faux positif au lancement
	
	// Garde en mémoire ceux qui parlaient à la dernière frame
	private HashSet<int> speakingLastFrame = new HashSet<int>();
	
	// Garde en mémoire ceux qui ont commencé à parler *pendant* que l'orateur parlait
	private HashSet<int> interrupters = new HashSet<int>();

	private void Awake()
	{
		announcementFeedback = GetComponent<AudioSource>();
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_SetIsSpeaking(int index, bool isSpeaking)
	{
		switch (index)
		{
			case 0:
				isLecteurSpeaking = isSpeaking;
				break;
			case 1:
				isCalculateurSpeaking = isSpeaking;
				break;
			case 2:
				isModelisateurSpeaking = isSpeaking;
				break;
		}
	}

	public void SetIsSpeaking(int index, bool isSpeaking)
	{
		// Au lieu de modifier la variable localement, on demande au serveur de le faire via RPC
		RPC_SetIsSpeaking(index, isSpeaking);
	}

	public override void Render()
	{
		if (!Runner.IsRunning) return;
		if (text != null)
		{
			text.GetComponent<TextMeshProUGUI>().text = stringToDisplay;
			text.gameObject.SetActive(isShowingFeedback);
		}
	}

	public override void FixedUpdateNetwork()
	{
		if (!Runner.IsRunning) return;

		if (!HasStateAuthority)
			return;

		isSpeakingThisFrame = new bool[] { isLecteurSpeaking, isCalculateurSpeaking, isModelisateurSpeaking };	

		// 1. Récupérer l'état actuel de tous les interlocuteurs avec la tolérance de silence
		HashSet<int> speakingThisFrame = new HashSet<int>();
		for (int i = 0; i < isSpeakingThisFrame.Length; i++)
		{
			if (!isSpeakingThisFrame[i])
				continue;

			if (isSpeakingThisFrame[i])
			{
				silenceTimers[i] = 0f; // Il parle, on reset le timer de silence
				speakingThisFrame.Add(i);
			}
			else
			{
				silenceTimers[i] += Time.deltaTime; // Il ne parle pas, on augmente le timer
				
				// Si le délai de pause n'est pas dépassé, on fait comme s'il parlait toujours
				if (silenceTimers[i] <= noPauseTiming)
				{
					speakingThisFrame.Add(i);
				}
			}
		}

		// 2. Détecter de NOUVELLES prises de parole
		foreach (int player in speakingThisFrame)
		{
			if (!speakingLastFrame.Contains(player))
			{
				// Silence avant -> Ce joueur prend la parole
				if (currentSpeaker == -1)
				{
					currentSpeaker = player;
				}
				// Quelqu'un parlait déjà -> Ce joueur essaie d'interrompre
				else
				{
					interrupters.Add(player);
				}
			}
		}

		// 3. Détecter des ARRÊTS de parole
		foreach (int player in speakingLastFrame)
		{
			if (!speakingThisFrame.Contains(player))
			{
				if (player == currentSpeaker)
				{
					// L'orateur principal s'est tu. A-t-il été interrompu ?
					
					// On filtre pour ne garder que les interrupteurs qui parlent encore
					interrupters.IntersectWith(speakingThisFrame);

					if (interrupters.Count > 0)
					{
						// OUI : Quelqu'un parlait en même temps et continue de parler !
						int whoTookTheFloor = -1;
						foreach(int interrupter in interrupters) 
						{
							whoTookTheFloor = interrupter;
							hasJustInterrupted = whoTookTheFloor;
							break; // On sélectionne le premier interrupteur valide
						}

						// Le "voleur de parole" devient le nouvel orateur principal
						currentSpeaker = whoTookTheFloor;
						interrupters.Remove(whoTookTheFloor);
					}
					else
					{
						// NON : Tout le monde s'est tu.
						currentSpeaker = -1;
					}
				}
				else
				{
					// Un interrupteur s'est tu avant que l'orateur principal s'arrête (tentative ratée)
					interrupters.Remove(player);
				}
			}
		}

		// Sécurité : Nettoyer la liste des interrupteurs pour être sûr de ne garder que ceux qui parlent vraiment
		interrupters.IntersectWith(speakingThisFrame);
		
		if (currentSpeaker != -1 && !speakingThisFrame.Contains(currentSpeaker))
		{
			currentSpeaker = -1; // Sécurité si l'orateur disparaît des radars d'une autre façon
		}

		// Préparation pour la prochaine frame
		speakingLastFrame = speakingThisFrame;
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_ShowFeedbackInterruptionToAll(int index)
	{
		string name = "";
		switch (index)
		{
			case 0:
				name = "Lecteur";
				break;
			case 1:
				name = "Calculateur";
				break;
			case 2:
				name = "Modélisateur";
				break;
		}
		stringToDisplay = $"Attention {name}, \nvous coupez trop la parole !";
		//text.gameObject.SetActive(true);
		isShowingFeedback = true;
		if (announcementFeedback != null && !announcementFeedback.isPlaying)
		{
			announcementFeedback.Play();
		}
		StartCoroutine(DisableFeedback());
	}

	private IEnumerator DisableFeedback()
	{
		yield return new WaitForSeconds(10f);
		stringToDisplay = "";
		isShowingFeedback = false;
		//text.gameObject.SetActive(false);
	}

	public override void Spawned()
	{
		Debug.Log("[Emery] SpeakingInterruption properly Spawned via Fusion.");

		// Pour un objet de scène unique (comportement de Singleton)
		if (SpeakingInterruption.Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			// Si un autre existe déjà en scène, on retire silencieusement celui-ci du réseau
			Runner.Despawn(Object);
		}
	}

	public int GetLastInterrupter()
	{
		return hasJustInterrupted;
	}

	public void ResetLastInterrupter()
	{
		RPC_ResetLastInterrupter();
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_ResetLastInterrupter()
	{
		hasJustInterrupted = -1;
	}
}
