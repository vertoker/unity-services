using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using System.Collections;
using UnityEngine;
using System;

/// <summary>
/// THIS SCRIPT NOT FINISH AND NOT TESTED YET, PLEASE DON'T USE
/// </summary>
namespace Game.Services
{
    public class IAPManager : MonoBehaviour, IStoreListener
    {
        [SerializeField] private ProductData[] products;
        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;
        public static IAPManager instance;

        void Awake()
        {
            instance = this;
        }
        void Start()
        {
            InitializePurchasing();
        }
        void InitializePurchasing()
        {
            if (IsInitialized()) { return; }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            for (int i = 0; i < products.Length; i++)
            {
                builder.AddProduct(products[i].id, products[i].productType,
                    new IDs() { { products[i].idAppStore, AppleAppStore.Name }, { products[i].idGooglePlay, GooglePlay.Name } });
            }
            UnityPurchasing.Initialize(this, builder);
        }
        bool IsInitialized()
        {
            return _storeController != null && _storeExtensionProvider != null;
        }

        public void SetDelegate(int id, PurchasingDelegate purchasing)
        {
            products[id].purchasing = purchasing;
        }
        public void BuyProductID(int id)
        {
            try
            {
                if (IsInitialized())
                {
                    Product product = _storeController.products.WithID(products[id].id);

                    if (product != null && product.availableToPurchase)
                    {
                        Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                        _storeController.InitiatePurchase(product);
                    }
                    else
                    {
                        Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                    }
                }
                else
                {
                    Debug.Log("BuyProductID FAIL. Not initialized.");
                }
            }
            catch (Exception e)
            {
                Debug.Log("BuyProductID: FAIL. Exception during purchase. " + e);
            }
        }
        public void RestorePurchases()
        {
            if (!IsInitialized()) { return; }// Not initialized

            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("RestorePurchases started ...");

                var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result) =>
                {
                    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            }
            else
            {
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (string.Equals(args.purchasedProduct.definition.id, products[i].id, StringComparison.Ordinal))
                { if (products[i].purchasing != null) { products[i].purchasing(); SceneManager.LoadScene(0); } break; }
            }
            return PurchaseProcessingResult.Complete;
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeExtensionProvider = extensions;
        }
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.Log("Initialize Failed:" + error);
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }
    }

    [Serializable]
    public class ProductData
    {
        public string id = "remove_ads";
        public string idAppStore { get { return "as_" + id; } }
        public string idGooglePlay { get { return "gp_" + id; } }

        public ProductType productType = ProductType.NonConsumable;
        public PurchasingDelegate purchasing = null;
    }
    public delegate void PurchasingDelegate();
}