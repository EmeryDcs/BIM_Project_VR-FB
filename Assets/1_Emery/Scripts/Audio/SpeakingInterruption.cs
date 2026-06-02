using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeakingInterruption : NetworkBehaviour
{
	public static SpeakingInterruption Instance { get; private set; }

	[Tooltip("L'index de l'orateur principal (celui à qui on peut couper la parole). -1 = personne.")]
	[SerializeField]
	private int currentSpeaker = -1;

	[Tooltip("Le temps (en secondes) pendant lequel le joueur peut faire une pause sans perdre la parole.")]
	[SerializeField]
	private float noPauseTiming = 0.5f;

	[Tooltip("0 = Lecteur, 1 = Calculateur, 2 = Modélisateur.")]
	private DetectSpeaking[] isSpeakingArray = new DetectSpeaking[3];
	
	// Permet de tracker le temps de silence de chaque joueur pour tolérer les pauses
	private float[] silenceTimers = new float[] { 999f, 999f, 999f }; // Initialisé grand pour éviter un faux positif au lancement
	
	// Garde en mémoire ceux qui parlaient à la dernière frame
	private HashSet<int> speakingLastFrame = new HashSet<int>();
	
	// Garde en mémoire ceux qui ont commencé à parler *pendant* que l'orateur parlait
	private HashSet<int> interrupters = new HashSet<int>();

	public void AffectSpeakerToTab(int index, DetectSpeaking script)
	{
		isSpeakingArray[index] = script;
	}

	public bool IsSpeakingSetInTab(int index)
	{
		return isSpeakingArray[index] != null;
	}

	public override void Render()
	{
		if (!Runner.IsRunning) return;

		// 1. Récupérer l'état actuel de tous les interlocuteurs avec la tolérance de silence
		HashSet<int> speakingThisFrame = new HashSet<int>();
		for (int i = 0; i < isSpeakingArray.Length; i++)
		{
			if (isSpeakingArray[i] == null)
				continue;

			if (isSpeakingArray[i].GetIsSpeaking())
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
							break; // On sélectionne le premier interrupteur valide
						}

						// ---> C'EST ICI QUE TU ENVOIES TON FEEDBACK <---
						Debug.Log($"[FEEDBACK] Le joueur {whoTookTheFloor} a coupé la parole au joueur {player} !");
						isSpeakingArray[whoTookTheFloor].ShowFeedback(); // Affiche le feedback visuel sur le joueur qui a coupé la parole
						// évolution vers -> int incrémental qui évalue cb de fois le mec à couper la parole, et feedback envoyé si x fois

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
}
