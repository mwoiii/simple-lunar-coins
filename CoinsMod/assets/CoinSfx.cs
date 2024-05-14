using BepInEx;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using System;
using System.Reflection;
using BepInEx.Configuration;
using RoR2.ContentManagement;
using System.Collections;
using Path = System.IO.Path;

public class CoinSfx : MonoBehaviour
{

    public void Start()
    {
        AkSoundEngine.PostEvent(4043138392, base.gameObject);
    }

}