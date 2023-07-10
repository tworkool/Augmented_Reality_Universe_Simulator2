using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;
using LightType = UnityEngine.LightType;

[ExecuteInEditMode]
public class CustomSunLight : MonoBehaviour
{
    [SerializeField]
    private FalloffType falloffType;

    public void OnEnable()
    {
        Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                Light l = requests[i];
                LightDataGI ld = new LightDataGI();

                switch (l.type)
                {
                    case LightType.Directional:
                        DirectionalLight dLight = new DirectionalLight();
                        LightmapperUtils.Extract(l, ref dLight);
                        ld.Init(ref dLight);
                        break;
                    case LightType.Point:
                        PointLight point = new PointLight();
                        LightmapperUtils.Extract(l, ref point);
                        ld.Init(ref point);
                        break;
                    case LightType.Spot:
                        SpotLight spot = new SpotLight();
                        LightmapperUtils.Extract(l, ref spot);
                        ld.Init(ref spot);
                        break;
                    case LightType.Area:
                        RectangleLight rect = new RectangleLight();
                        LightmapperUtils.Extract(l, ref rect);
                        ld.Init(ref rect);
                        break;
                    case LightType.Disc:
                        DiscLight disc = new DiscLight();
                        LightmapperUtils.Extract(l, ref disc);
                        ld.Init(ref disc);
                        break;
                    default:
                        ld.InitNoBake(l.GetInstanceID());
                        break;
                }

                ld.cookieID = l.cookie?.GetInstanceID() ?? 0;
                ld.falloff = falloffType;
                lightsOutput[i] = ld;
            }
        };

        Lightmapping.SetDelegate(lightsDelegate);
    }

    void OnDisable()
    {
        Lightmapping.ResetDelegate();
    }
}
