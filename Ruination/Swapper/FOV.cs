using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CUE4Parse.UE4.Assets;
using Ruination_v2.Utils;

namespace Ruination_v2.Swapper;

public class FOV
{

        public static async Task<bool> SwapFOV(int fov)
        {
            try
            {
                Logger.Log("Converting FOV " + fov);

                //Ik 3 exports are done very well!!!!!!!!!!
                var fovPackage = (IoPackage) await SwapUtils.GetProvider().LoadPackageAsync("FortniteGame/Content/Blueprints/Camera/Athena/Athena_PlayerCameraModeBase");
                var fovObjects = await SwapUtils.GetProvider().LoadAllObjectsAsync("FortniteGame/Content/Blueprints/Camera/Athena/Athena_PlayerCameraModeBase");
                var fovBytes = await SwapUtils.GetProvider().SaveAssetAsync("FortniteGame/Content/Blueprints/Camera/Athena/Athena_PlayerCameraModeBase");
                
                var fovObject = fovObjects.FirstOrDefault();

                var fovProperty = fovObject.Properties.FirstOrDefault(x => x.Name == "FOV");

                if(fovProperty == null)
                {
                    MessageBox.Show("No FOV Property was found.");
                    Logger.Log("No FOV Property found");
                    return false;
                }

                Logger.Log($"FOV Property found at {fovProperty.Position} with Size {fovProperty.Size}");

                List<byte> bytes = new List<byte>(fovBytes);
                bytes.RemoveRange(fovProperty.Position, fovProperty.Size);
                bytes.InsertRange(fovProperty.Position, BitConverter.GetBytes((float)fov));

                if(!await SwapUtils.SwapAsset(fovPackage, bytes.ToArray()))
                    return false;

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.Type == "FOV");
                Config.GetConfig().ConvertedItems.Add(new()
                {
                    Type = "FOV",
                    Assets = new(),
                    ID = "FOV",
                    isPlugin = false,
                    Name = fov + " FOV",
                    OptionID = "FOV"
                });

                Config.Save();

                MessageBox.Show("Converted FOV to " + fov);

                return true;
            } catch(Exception e)
            {
                MessageBox.Show("There is an error: " + e.Message);
                Logger.LogError(e.Message, e);
                return false;
            }
        }

        public static async Task <bool> RevertFov(bool showMsgbox = true)
        {
            try
            {
                Logger.Log("Reverting FOV");

                var fovPackage = (IoPackage)await SwapUtils.GetProvider().LoadPackageAsync("FortniteGame/Content/Blueprints/Camera/Athena/Athena_PlayerCameraModeBase");

                if (!await SwapUtils.RevertPackage(fovPackage))
                    return false;

                if(showMsgbox)
                {
                    Config.GetConfig().ConvertedItems.RemoveAll(x => x.Type == "FOV");
                    Config.Save();
                    MessageBox.Show("Reverted FOV");
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("There is an error: " + e.Message);
                Logger.LogError(e.Message, e);
                return false;
            }
        }

    }
