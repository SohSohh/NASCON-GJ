using System.Collections;
using NasconGJ.Items;
using NasconGJ.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NasconGJ.Core
{
    /// <summary>
    /// What crosses from one level into the next through a passage: where you stood inside the passage, which way you
    /// looked, and what you carried (and how much fuel was left).
    /// </summary>
    public static class Transit
    {
        public static bool Pending;
        public static Vector3 LocalPosition;
        public static float LocalYaw, Pitch;
        public static string ItemName;
        public static float Fuel;
        public static bool Burning;
        /// <summary>Frame on which a level was entered through a passage (that level starts without its fade-in).</summary>
        public static int ArrivedFrame = -1;
        /// <summary>Prefab of the item to hand back on arrival (set by the arrival passage, read by the inventory).</summary>
        public static Items.CarryableItem CarriedPrefab;
        /// <summary>True only during the frame a level is entered through a passage.</summary>
        public static bool ArrivingNow => ArrivedFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Pending = false;
    }

    /// <summary>
    /// A twisting crawlspace that joins two levels without a cut. Every level has the same passage: at its exit (role
    /// Exit) and behind its start (role Arrival). Walking into the exit passage starts loading the next level in the
    /// background; once you're round the bend (nothing of the level in sight) it switches, and you're standing at the
    /// identical spot of the identical passage in the next level, same view, same item, same fuel, and walk on out.
    /// The arrival passage also leaves a fresh torch by its doorway: the checkpoint.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class LevelPassage : MonoBehaviour
    {
        public enum Role { Exit, Arrival }

        [SerializeField] private Role role;
        public bool IsExit => role == Role.Exit;
        [Tooltip("Exit: the scene to load (empty = next in story order).")]
        [SerializeField] private string nextScene = "";
        [SerializeField] private Collider enterTrigger;    // Exit: start loading here
        [SerializeField] private Collider swapTrigger;     // Exit: round the bend, switch here
        [SerializeField] private Collider blocker;         // Exit: closed until the level's exit opens
        [SerializeField] private Puzzles.LevelExit levelExit;
        [SerializeField] private CarryableItem[] itemPrefabs;   // Arrival: what you might be carrying
        [SerializeField] private Transform freshTorchPoint;     // Arrival: the checkpoint torch

        private AsyncOperation loading;
        private bool swapping;
        private Transform player;

        private void Awake()
        {
            if (role == Role.Arrival) Arrive();
        }

        private void Start()
        {
            if (role != Role.Exit) return;
            foreach (var c in new[] { enterTrigger, swapTrigger }) if (c != null) { c.isTrigger = true; c.gameObject.layer = 2; }
        }

        private void Update()
        {
            if (role != Role.Exit) return;
            if (blocker != null) blocker.enabled = levelExit != null && !levelExit.IsOpen;
            if (player == null) { var go = GameObject.FindWithTag(GameTags.Player); if (go != null) player = go.transform; }
            if (player == null) return;
            // by position, every frame (trigger events can be missed)
            Vector3 p = player.position + Vector3.up;
            if (swapTrigger != null && swapTrigger.bounds.Contains(p)) OnPlayerEntered(player, true);
            else if (enterTrigger != null && enterTrigger.bounds.Contains(p)) OnPlayerEntered(player, false);
            if (swapping) Record(player);   // keep the latest pose until the old scene goes away
        }

        // ------------------------------------------------------------------ exit

        internal void OnPlayerEntered(Transform who, bool atSwap)
        {
            if (levelExit != null && !levelExit.IsOpen) return;
            player = who;
            if (loading == null)
            {
                string next = string.IsNullOrEmpty(nextScene) ? NextInStory() : nextScene;
                Application.backgroundLoadingPriority = ThreadPriority.Low;   // stream it in without hitching the walk
                loading = SceneManager.LoadSceneAsync(next, LoadSceneMode.Single);
                loading.allowSceneActivation = false;
            }
            if (atSwap && !swapping) StartCoroutine(Swap());
        }

        private IEnumerator Swap()
        {
            swapping = true;
            Record(player);
            Transit.Pending = true;
            while (loading.progress < 0.9f) yield return null;   // normally long done by now
            Application.backgroundLoadingPriority = ThreadPriority.High;  // and finish the switch in as few frames as possible
            loading.allowSceneActivation = true;
        }

        private void Record(Transform who)
        {
            Vector3 local = transform.InverseTransformPoint(who.position);
            Transit.LocalPosition = local;
            Transit.LocalYaw = Mathf.DeltaAngle(transform.eulerAngles.y, who.eulerAngles.y);
            var ctrl = who.GetComponent<FirstPersonController>();
            if (ctrl != null && ctrl.CameraRoot != null) Transit.Pitch = Mathf.DeltaAngle(0f, ctrl.CameraRoot.localEulerAngles.x);
            var inv = who.GetComponent<PlayerInventory>();
            var item = inv != null ? inv.ActiveItem : null;
            Transit.ItemName = item != null ? BaseName(item.name) : null;
            Transit.Fuel = item != null && item.Flammable != null ? item.Flammable.NormalizedFuel : 0f;
            Transit.Burning = item != null && item.Flammable != null && item.Flammable.IsBurning;
        }

        /// <summary>"PF_Torch (2)" / "PF_Torch(Clone)" -> "PF_Torch".</summary>
        private static string BaseName(string n)
        {
            n = n.Replace("(Clone)", "");
            int k = n.IndexOf(" (");
            return (k > 0 ? n.Substring(0, k) : n).Trim();
        }

        private static string NextInStory()
        {
            int i = System.Array.IndexOf(SceneNames.StoryOrder, SceneManager.GetActiveScene().name);
            return i < 0 || i + 1 >= SceneNames.StoryOrder.Length ? SceneNames.MainMenu : SceneNames.StoryOrder[i + 1];
        }

        // ------------------------------------------------------------------ arrival

        private void Arrive()
        {
            if (freshTorchPoint != null && itemPrefabs != null && itemPrefabs.Length > 0 && itemPrefabs[0] != null)
                Instantiate(itemPrefabs[0], freshTorchPoint.position, freshTorchPoint.rotation);   // the checkpoint torch
            if (!Transit.Pending) return;
            Transit.Pending = false;
            Transit.ArrivedFrame = Time.frameCount;
            var go = GameObject.FindWithTag(GameTags.Player);
            if (go == null) return;
            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            go.transform.SetPositionAndRotation(transform.TransformPoint(Transit.LocalPosition), Quaternion.Euler(0f, transform.eulerAngles.y + Transit.LocalYaw, 0f));
            if (cc != null) cc.enabled = true;
            Transit.CarriedPrefab = null;
            if (!string.IsNullOrEmpty(Transit.ItemName) && itemPrefabs != null)
                foreach (var p in itemPrefabs) if (p != null && p.name == Transit.ItemName) Transit.CarriedPrefab = p;
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
        }
    }
}
