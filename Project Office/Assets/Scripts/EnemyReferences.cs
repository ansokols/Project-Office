using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class EnemyReferences : MonoBehaviour
{
    [field: Header("Enemy Objects")]
    [field: SerializeField] public GameObject vision {get; private set;}
    [field: SerializeField] public GameObject ammoBoxPrefab {get; private set;}
    [field: SerializeField] public GameObject healBoxPrefab {get; private set;}
    [field: SerializeField] public Image restartButton {get; private set;}
    [field: SerializeField] public Text restartText {get; private set;}
    [field: SerializeField] public Text winText {get; private set;}
    [field: SerializeField] public GameObject hud {get; private set;}
    [field: SerializeField] public GameObject player {get; private set;}
    [field: SerializeField] public Transform[] waypoints {get; private set;}
    [field: SerializeField] public List<Transform> locations {get; private set;}
    //[field: SerializeField] public Transform[] locations {get; private set;}

    [field: Header("Shooting Objects")]
    [field: SerializeField] public Transform firePoint {get; private set;}
    [field: SerializeField] public GameObject bulletPrefab {get; private set;}
    [field: SerializeField] public GameObject shellPrefab {get; private set;}
    [field: SerializeField] public GameObject flashEffect {get; private set;}

    [field: Header("Audio")]
    [field: SerializeField] public AudioSource movementAudioSource {get; private set;}
    [field: SerializeField] public AudioClip walkingSFX {get; private set;}
    [field: SerializeField] public AudioClip runningSFX {get; private set;}
    [field: SerializeField] public AudioSource playerAudioSource {get; private set;}
    [field: SerializeField] public AudioClip shotSFX {get; private set;}
    [field: SerializeField] public AudioClip reloadSFX {get; private set;}
    [field: SerializeField] public AudioClip shellsSFX {get; private set;}

    public Animator legsAnim {get; private set;}
    public Animator bodyAnim {get; private set;}
    public NavMeshAgent agent {get; private set;}

    void Awake()
    {
        legsAnim = GetComponent<Animator>();
        bodyAnim = transform.Find("Body").gameObject.GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }
}
