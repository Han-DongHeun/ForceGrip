using System.Collections.Generic;
using UnityEngine;

public record UVCoords
{
    public struct UVInfos
    {
        public Vector2 tiling;
        public Vector2 offset;
        public Vector3 rotation;
    }
    
    public readonly Dictionary<string, List<UVInfos>> PickAndPlaceTaskObjectUVInfos;
    
    public UVCoords()
    {
        PickAndPlaceTaskObjectUVInfos = new Dictionary<string, List<UVInfos>>
        {
            {
                "sphere", new List<UVInfos>
                {
                    new UVInfos
                    {
                        tiling = new Vector2(15f, 15f),
                        offset = new Vector2(-3.2f, -7f),
                        rotation = new Vector3(-90f, 0f, 0f)
                    }
                }
            },
            {
                "cubesmall", new List<UVInfos>
                {
                    new UVInfos
                    {
                        tiling = new Vector2(15f, 15f),
                        offset = new Vector2(-2f, -2f),
                        rotation = new Vector3(0f, 0f, 0f)
                    }
                }
            },
            {
                "cylindersmall", new List<UVInfos>
                {
                    new UVInfos
                    {
                        tiling = new Vector2(15f, 15f),
                        offset = new Vector2(-2f, -2f),
                        rotation = new Vector3(-90f, 0f, 0f)
                    }
                }
            }
        };
    }
}