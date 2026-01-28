using System;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using Ktisis.Core.Attributes;
using Ktisis.Services.Game;

namespace Ktisis.Services.Data;

[Transient]
public unsafe class HousingDataService : IDisposable
{
    private HousingManager* _housingManager;
    private readonly GPoseService _gposeService;
    private readonly SSAOHook _ssaoHook;

    public bool IsInHousing => this._housingManager != null && this._housingManager->IsInside();
    public IndoorTerritory* IndoorTerritory => _housingManager->IndoorTerritory;

    public float IndoorLight
    {
        get
        {
            if (!IsInHousing || IndoorTerritory == null)
                return float.NaN;
            
            return IndoorTerritory->BrightnessTarget;
        }
        set
        {
            if (!IsInHousing || IndoorTerritory == null)
                return;
            
            float speed = value - IndoorTerritory->BrightnessCurrent;
            
            IndoorTerritory->BrightnessTarget = value;
            IndoorTerritory->BrightnessTransitionSpeed = speed;
            IndoorTerritory->IsBrightnessTransitioning = true;
        }
    }

    public bool SSAOEnabled
    {
        get => this._housingManager != null && IndoorTerritory != null && IndoorTerritory->SSAOEnable;
        set {
            if (!IsInHousing || IndoorTerritory == null)
                return;
            
            this._ssaoHook.Set(value);
        }
    }

    public HousingDataService(GPoseService gPoseService, SSAOHook ssaoHook)
    {
        this._housingManager = HousingManager.Instance();
        this._gposeService = gPoseService;
        this._ssaoHook = ssaoHook;
        
        gPoseService.StateChanged += GPoseServiceOnStateChanged;
    }
    
    public void Dispose()
    {
        this._gposeService.StateChanged -= GPoseServiceOnStateChanged;
    }
    
    
    internal void ResetLighting()
    {
        if (!IsInHousing || IndoorTerritory == null)
            return;
        
        float target = 1.0f - (IndoorTerritory->SavedInvertedBrightness * 0.2f);
        float speed = Math.Sign(IndoorTerritory->BrightnessCurrent - target) * 0.02f;
            
        IndoorTerritory->BrightnessTarget = target;
        IndoorTerritory->BrightnessTransitionSpeed = speed;
        IndoorTerritory->IsBrightnessTransitioning = true;
    }
    
    public void ResetSSAO()
    {
        if (!IsInHousing || IndoorTerritory == null)
            return;
        
        this.SSAOEnabled = IndoorTerritory->SavedSSAOEnable;
    }
    
    private void GPoseServiceOnStateChanged(GPoseService sender, bool state)
    {
        if (state)
            OnEnabled();
        else
            OnDisabled();
    }
    
    private void OnDisabled()
    {
        ResetLighting();
        ResetSSAO();
    }

    private void OnEnabled()
    {
        this._housingManager = HousingManager.Instance();
    }
    
    [Singleton]
    public unsafe class SSAOHook
    {
        private delegate nint ToggleSSAO(HousingManager* Instance, bool option);
        
        [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B 79 ?? 0F B6 DA")]
        private readonly ToggleSSAO toggle = null;

        public SSAOHook(IGameInteropProvider interopProvider)
        {
            interopProvider.InitializeFromAttributes(this);
        }

        public void Set(bool state)
        {
            toggle.Invoke(HousingManager.Instance(), state);
        }
    }
}
