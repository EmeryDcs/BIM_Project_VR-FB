using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeakingInterruption : MonoBehaviour
{
	[Tooltip("L'index de l'orateur principal (celui à qui on peut couper la parole). -1 = personne.")]
	[SerializeField]
	private int currentSpeaker = -1;

	private List<DetectSpeaking> list_SC_detectSpeaking = new List<DetectSpeaking>();
	
	// Garde en mémoire ceux qui parlaient à la dernière frame
	private HashSet<int> speakingLastFrame = new HashSet<int>();
	
	// Garde en mémoire ceux qui ont commencé à parler *pendant* que l'orateur parlait
	private HashSet<int> interrupters = new HashSet<int>();

	public void GetAllSCDetectSpeaking()
	{
		list_SC_detectSpeaking.Clear();
		foreach (DetectSpeaking script in FindObjectsOfType<DetectSpeaking>())
		{
			list_SC_detectSpeaking.Add(script);
		}
	}

	private void Update()
	{
		if (list_SC_detectSpeaking.Count == 0) return;

		// 1. Récupérer l'état actuel de tous les interlocuteurs
		HashSet<int> speakingThisFrame = new HashSet<int>();
		for (int i = 0; i < list_SC_detectSpeaking.Count; i++)
		{
			if (list_SC_detectSpeaking[i].GetIsSpeaking())
			{
				speakingThisFrame.Add(i);
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
						list_SC_detectSpeaking[whoTookTheFloor].ShowFeedback(); // Affiche le feedback visuel sur le joueur qui a coupé la parole

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
}
