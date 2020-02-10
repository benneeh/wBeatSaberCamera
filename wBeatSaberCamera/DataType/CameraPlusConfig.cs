using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using wBeatSaberCamera.Models;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.DataType
{
    [DataContract]
    public sealed class CameraPlusConfig : DirtyBase
    {
        private float _fov = 90;
        private int _antiAliasing = 2;
        private float _rotX = 15;
        private float _rotY = -15;
        private float _rotZ;
        private float _x = 1;
        private float _y = 2.5f;
        private float _z = -1.9f;
        private float _renderScale = 1;
        private bool _thirdPerson = true;
        private float _rotationSmooth = 1;
        private float _positionSmooth = 1;
        private int _screenWidth = 1920;
        private int _screenHeight = 1080;
        private int _screenPosX;
        private int _screenPosY;
        private int _layer = 1;
        private string _movementScriptPath = "";
        private bool _showThirdPersonCamera;
        private bool _fitToCanvas;
        private bool _makeWallsTransparent;
        private float _firstPersonPosOffsetX;
        private float _firstPersonPosOffsetY;
        private float _firstPersonPosOffsetZ;
        private float _firstPersonRotOffsetX;
        private float _firstPersonRotOffsetY;
        private float _firstPersonRotOffsetZ;
        private bool _forceFirstPersonUpright;

        /// <summary>
        /// Horizontal field of view of the camera
        /// </summary>
        [DataMember(Name = "fov")]
        public float FieldOfView
        {
            get => _fov;
            set
            {
                if (value.Equals(_fov))
                {
                    return;
                }

                _fov = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Anti-aliasing setting for the camera (1, 2, 4 or 8 only)
        /// </summary>
        [DataMember(Name = "antiAliasing")]
        public int AntiAliasing
        {
            get => _antiAliasing;
            set
            {
                if (value == _antiAliasing)
                {
                    return;
                }

                _antiAliasing = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The resolution scale of the camera relative to game window (similar to supersampling for VR)
        /// </summary>
        [DataMember(Name = "renderScale")]
        public float RenderScale
        {
            get => _renderScale;
            set
            {
                if (value.Equals(_renderScale))
                {
                    return;
                }

                _renderScale = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// How much position should smooth (SMALLER NUMBER = SMOOTHER)
        /// </summary>
        [DataMember(Name = "positionSmooth")]
        public float PositionSmoothening
        {
            get => _positionSmooth;
            set
            {
                if (value.Equals(_positionSmooth))
                {
                    return;
                }

                _positionSmooth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// How much rotation should smooth (SMALLER NUMBER = SMOOTHER)
        /// </summary>
        [DataMember(Name = "rotationSmooth")]
        public float RotationSmoothening
        {
            get => _rotationSmooth;
            set
            {
                if (value.Equals(_rotationSmooth))
                {
                    return;
                }

                _rotationSmooth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether third person camera is enabled
        /// </summary>
        [DataMember(Name = "thirdPerson")]
        public bool IsThirdPersonView
        {
            get => _thirdPerson;
            set
            {
                if (value == _thirdPerson)
                {
                    return;
                }

                _thirdPerson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFirstPersonView));
            }
        }

        public bool IsFirstPersonView
        {
            get => !_thirdPerson;
            set
            {
                if (value == !_thirdPerson)
                {
                    return;
                }

                _thirdPerson = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsThirdPersonView));
            }
        }

        /// <summary>
        /// Whether or not the third person camera is visible
        /// </summary>
        [DataMember(Name = "showThirdPersonCamera")]
        public bool ShowThirdPersonCamera
        {
            get => _showThirdPersonCamera;
            set
            {
                if (value == _showThirdPersonCamera)
                {
                    return;
                }

                _showThirdPersonCamera = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// X position of third person camera
        /// </summary>
        [DataMember(Name = "posx")]
        public float X
        {
            get => _x;
            set
            {
                if (value.Equals(_x))
                {
                    return;
                }

                _x = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Y position of third person camera
        /// </summary>
        [DataMember(Name = "posy")]
        public float Y
        {
            get => _y;
            set
            {
                if (value.Equals(_y))
                {
                    return;
                }

                _y = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Z position of third person camera
        /// </summary>
        [DataMember(Name = "posz")]
        public float Z
        {
            get => _z;
            set
            {
                if (value.Equals(_z))
                {
                    return;
                }

                _z = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// X rotation of third person camera
        /// </summary>
        [DataMember(Name = "angx")]
        public float RotX
        {
            get => _rotX;
            set
            {
                if (value.Equals(_rotX))
                {
                    return;
                }

                _rotX = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Y rotation of third person camera
        /// </summary>
        [DataMember(Name = "angy")]
        public float RotY
        {
            get => _rotY;
            set
            {
                if (value.Equals(_rotY))
                {
                    return;
                }

                _rotY = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Z rotation of third person camera
        /// </summary>
        [DataMember(Name = "angz")]
        public float RotZ
        {
            get => _rotZ;
            set
            {
                if (value.Equals(_rotZ))
                {
                    return;
                }

                _rotZ = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// X position offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonPosOffsetX")]
        public float FirstPersonPosOffsetX
        {
            get => _firstPersonPosOffsetX;
            set
            {
                if (value.Equals(_firstPersonPosOffsetX))
                {
                    return;
                }

                _firstPersonPosOffsetX = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Y position offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonPosOffsetY")]
        public float FirstPersonPosOffsetY
        {
            get => _firstPersonPosOffsetY;
            set
            {
                if (value.Equals(_firstPersonPosOffsetY))
                {
                    return;
                }

                _firstPersonPosOffsetY = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Z position offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonPosOffsetZ")]
        public float FirstPersonPosOffsetZ
        {
            get => _firstPersonPosOffsetZ;
            set
            {
                if (value.Equals(_firstPersonPosOffsetZ))
                {
                    return;
                }

                _firstPersonPosOffsetZ = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// X rotation offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonRotOffsetX")]
        public float FirstPersonRotOffsetX
        {
            get => _firstPersonRotOffsetX;
            set
            {
                if (value.Equals(_firstPersonRotOffsetX))
                {
                    return;
                }

                _firstPersonRotOffsetX = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Y rotation offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonRotOffsetY")]
        public float FirstPersonRotOffsetY
        {
            get => _firstPersonRotOffsetY;
            set
            {
                if (value.Equals(_firstPersonRotOffsetY))
                {
                    return;
                }

                _firstPersonRotOffsetY = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Z rotation offset of first person camera
        /// </summary>
        [DataMember(Name = "firstPersonRotOffsetZ")]
        public float FirstPersonRotOffsetZ
        {
            get => _firstPersonRotOffsetZ;
            set
            {
                if (value.Equals(_firstPersonRotOffsetZ))
                {
                    return;
                }

                _firstPersonRotOffsetZ = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Width of the camera render area
        /// </summary>
        [DataMember(Name = "screenWidth")]
        public int ScreenWidth
        {
            get => _screenWidth;
            set
            {
                if (value == _screenWidth)
                {
                    return;
                }

                _screenWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Height of the camera render area
        /// </summary>
        [DataMember(Name = "screenHeight")]
        public int ScreenHeight
        {
            get => _screenHeight;
            set
            {
                if (value == _screenHeight)
                {
                    return;
                }

                _screenHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// X position of the camera in the Beat Saber window
        /// </summary>
        [DataMember(Name = "screenPosX")]
        public int ScreenPosX
        {
            get => _screenPosX;
            set
            {
                if (value == _screenPosX)
                {
                    return;
                }

                _screenPosX = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Y position of the camera in the Beat Saber window
        /// </summary>
        [DataMember(Name = "screenPosY")]
        public int ScreenPosY
        {
            get => _screenPosY;
            set
            {
                if (value == _screenPosY)
                {
                    return;
                }

                _screenPosY = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Layer to render the camera on (HIGHER NUMBER = top)
        /// </summary>
        [DataMember(Name = "layer")]
        public int Layer
        {
            get => _layer;
            set
            {
                if (value == _layer)
                {
                    return;
                }

                _layer = value;
                OnPropertyChanged();
            }
        }

        [DataMember(Name = "fitToCanvas")]
        public bool FitToCanvas
        {
            get => _fitToCanvas;
            set
            {
                if (value == _fitToCanvas)
                {
                    return;
                }

                _fitToCanvas = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// no documentation, but probably makes walls show as transparent in the external camera
        /// </summary>
        [DataMember(Name = "transparentWalls")]
        public bool MakeWallsTransparent
        {
            get => _makeWallsTransparent;
            set
            {
                if (value == _makeWallsTransparent)
                {
                    return;
                }

                _makeWallsTransparent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Lock rotation of first person camera on Z axis to keep the camera upright
        /// </summary>
        [DataMember(Name = "forceFirstPersonUpRight")]
        public bool ForceFirstPersonUpright
        {
            get => _forceFirstPersonUpright;
            set
            {
                if (value == _forceFirstPersonUpright)
                {
                    return;
                }

                _forceFirstPersonUpright = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Path of the movement script associated with the camera
        /// </summary>
        [DataMember(Name = "movementScriptPath")]
        public string MovementScriptPath
        {
            get => _movementScriptPath;
            set
            {
                if (value == _movementScriptPath)
                {
                    return;
                }

                _movementScriptPath = value;
                OnPropertyChanged();
            }
        }

        public void SaveToBeatSaber(AppConfigModel appConfigModel)
        {
            lock (appConfigModel)
            {
                using (var fs = File.Open(appConfigModel.CameraPlusConfig, FileMode.Create, FileAccess.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var propertyInfo in properties)
                    {
                        var dma = propertyInfo.GetCustomAttribute<DataMemberAttribute>();
                        if (dma == null)
                        {
                            continue;
                        }

                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            sw.WriteLine($"{dma.Name ?? propertyInfo.Name}=\"{propertyInfo.GetValue(this)}\"");
                        }
                        else
                        {
                            sw.WriteLine($"{dma.Name ?? propertyInfo.Name}={propertyInfo.GetValue(this)}");
                        }
                    }
                }
            }
        }

        public void LoadDataFromBeatSaber(AppConfigModel appConfigModel)
        {
            lock (appConfigModel)
            {
                using (var fs = File.Open(appConfigModel.CameraPlusConfig, FileMode.Open, FileAccess.Read))
                using (var sw = new StreamReader(fs))
                {
                    var properties = typeof(CameraPlusConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var propertyDict = properties.ToDictionary(x => x.GetCustomAttribute<DataMemberAttribute>()?.Name ?? x.Name);

                    while (!sw.EndOfStream)
                    {
                        var line = sw.ReadLine();
                        if (line == null || line.StartsWith(";"))
                        {
                            continue;
                        }
                        var splitLine = line.Split('=');
                        if (splitLine.Length < 2)
                        {
                            continue;
                        }

                        var name = splitLine[0];
                        var value = splitLine[1];
                        if (value.Contains(";"))
                        {
                            value = value.Split(';')[0];
                        }
                        value = value.Trim();

                        if (!propertyDict.TryGetValue(name, out var property))
                        {
                            Log.Warn($"unmapped property '{name}' with value '{value}' found in the config");
                        }
                        else
                        {
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(this, value.Substring(1, value.Length - 2));
                            }
                            else
                            {
                                property.SetValue(this, Convert.ChangeType(value, property.PropertyType));
                            }
                        }
                    }
                }
            }
        }
    }
}