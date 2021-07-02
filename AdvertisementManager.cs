using System.Collections.Generic;
using UnityEngine.Advertisements;
using System.Collections;
using UnityEngine;

namespace Game.Services
{
    public class AdvertisementManager : MonoBehaviour, IUnityAdsListener
    {
        // Put your game ID here
        [SerializeField] private string idAppStore = "*******";
        [SerializeField] private string idPlayMarket = "*******";
        // Don't forget off testMode after release
        [SerializeField] private bool testMode = false;

        public static AdvertisementManager instance;
        private Dictionary<string, Placement> _videoPlacements = new Dictionary<string, Placement>();
        private bool _showing = false;

        private void Awake()
        {
            instance = this;
            Advertisement.AddListener(this);
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
                Advertisement.Initialize(idAppStore, testMode);
            else
                Advertisement.Initialize(idPlayMarket, testMode);
        }

        /// <summary>
        /// Set video or video rewarded placement (call on Start)
        /// </summary>
        /// <param name="placementId">ID of ad placement</param>
        /// <param name="ready">Call when ad is ready</param>
        /// <param name="start">Call when ad is start to show</param>
        /// <param name="finished">Call when the user has watched the ad to the end</param>
        /// <param name="skipped">Call when a user skipped ad</param>
        /// <param name="failed">Call when an ad didn't load due to some kind of error</param>
        public void SetVideo(string placementId, PlacementDelegate ready = null, PlacementDelegate start = null, PlacementDelegate finished = null, PlacementDelegate skipped = null, PlacementDelegate failed = null)
        {
            Placement placement = new Placement(ready, start, failed, finished, skipped);
            _videoPlacements.Add(placementId, placement);
        }
        /// <summary>
        /// Show video or video rewarded placement
        /// </summary>
        /// <param name="placementId">ID of ad placement</param>
        public void ShowVideo(string placementId)
        {
            if (!_showing && Advertisement.IsReady(placementId))
            {
                _showing = true;
                Advertisement.Show(placementId);
            }
        }
        /// <summary>
        /// Set and show banner placement (call on Start)
        /// </summary>
        /// <param name="placementId">ID of banner placement</param>
        public void SetShowBanner(string placementId)
        {
            BannerLoadOptions options = new BannerLoadOptions()
            { errorCallback = (string message) => { Debug.Log("banner error is " + message); } };
            Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
            Advertisement.Banner.Load(placementId);
            StartCoroutine(WaitLoadAndReadyBanner());

            IEnumerator WaitLoadAndReadyBanner()
            {
                while (!Advertisement.Banner.isLoaded && Advertisement.IsReady(placementId))
                    yield return new WaitForSecondsRealtime(0.25f);
                Advertisement.Banner.Show();
            }
        }

        // IUnityAdsListener realization
        public void OnUnityAdsReady(string placementId)
        {
            foreach (KeyValuePair<string, Placement> placement in _videoPlacements)
            {
                if (placementId == placement.Key)
                    placement.Value.Ready();
                break;
            }
        }
        public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
        {
#if UNITY_EDITOR
            Debug.Log(showResult);
#endif
            switch (showResult)
            {
                case ShowResult.Finished:
                    _videoPlacements[placementId].Finished();
                    break;
                case ShowResult.Skipped:
                    _videoPlacements[placementId].Skipped();
                    break;
                case ShowResult.Failed:
                    _videoPlacements[placementId].Failed();
                    break;
            }
            _showing = false;
        }
        public void OnUnityAdsDidError(string message)
        {
#if UNITY_EDITOR
            Debug.LogError(message);
#endif
        }
        public void OnUnityAdsDidStart(string placementId)
        {
            _videoPlacements[placementId].Start();
        }
        void OnDestroy()
        {
            Advertisement.RemoveListener(this);
        }
    }
    class Placement
    {
        private readonly PlacementDelegate _ready;
        private readonly PlacementDelegate _start;
        private readonly PlacementDelegate _failed;
        private readonly PlacementDelegate _finished;
        private readonly PlacementDelegate _skipped;

        public Placement(PlacementDelegate ready, PlacementDelegate start, PlacementDelegate failed, PlacementDelegate finished, PlacementDelegate skipped)
        {
            _ready = ready;
            _start = start;
            _failed = failed;
            _finished = finished;
            _skipped = skipped;
        }

        public void Ready() { _ready?.Invoke(); }
        public void Start() { _start?.Invoke(); }
        public void Failed() { _failed?.Invoke(); }
        public void Finished() { _finished?.Invoke(); }
        public void Skipped() { _skipped?.Invoke(); }
    }
    public delegate void PlacementDelegate();
}