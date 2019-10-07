using Microsoft.Xna.Framework.Audio;
using NTextCat;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Data;
using TwitchLib.Client.Models;
using Valve.VR;
using wBeatSaberCamera.DataType;
using wBeatSaberCamera.Utils;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class ChatConfigModel : DirtyBase
    {
        #region private fields

        private SpeechSynthesizer _synthesizer;

        private CVRSystem _vrSystem;
        private bool _isVrEnabled;
        private readonly AudioListener _audioListener;
        private ObservableDictionary<string, Chatter> _chatters;
        private bool _isTextToSpeechEnabled;
        private double _maxPitchFactor = .3;
        private NaiveBayesLanguageIdentifier _lazyLanguagesIdentifier;
        private static readonly Random s_random = new Random();

        private readonly TaskSerializer _taskSerializer = new TaskSerializer();
        private bool _isSendMessagesEnabled;
        private bool _isReadingStreamerMessagesEnabled;
        private bool _isSpeechToTextEnabled;

        #endregion private fields

        #region properties

        [DataMember]
        public ObservableDictionary<string, Chatter> Chatters
        {
            get => _chatters;
            set
            {
                if (Equals(value, _chatters))
                {
                    return;
                }

                UnsubscribeDirtyCollection(_chatters);
                _chatters = value;
                SubscribeDirtyCollection(_chatters);

                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsTextToSpeechEnabled
        {
            get => _isTextToSpeechEnabled;
            set
            {
                if (value == _isTextToSpeechEnabled)
                {
                    return;
                }

                _isTextToSpeechEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public double MaxPitchFactor
        {
            get => _maxPitchFactor;
            set
            {
                if (value.Equals(_maxPitchFactor))
                {
                    return;
                }

                _maxPitchFactor = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsSendMessagesEnabled
        {
            get => _isSendMessagesEnabled;
            set
            {
                if (value == _isSendMessagesEnabled)
                    return;

                _isSendMessagesEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsReadingStreamerMessagesEnabled
        {
            get => _isReadingStreamerMessagesEnabled;
            set
            {
                if (value == _isReadingStreamerMessagesEnabled)
                    return;

                _isReadingStreamerMessagesEnabled = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool IsSpeechToTextEnabled
        {
            get => _isSpeechToTextEnabled;
            set
            {
                if (value == _isSpeechToTextEnabled)
                    return;

                _isSpeechToTextEnabled = value;
                OnPropertyChanged();
            }
        }

        #endregion properties

        public ChatConfigModel()
        {
            Chatters = new ObservableDictionary<string, Chatter>();
            BindingOperations.EnableCollectionSynchronization(Chatters, new object());

            EVRInitError evrInitError = default;
            Task.Run(() =>
            {
                // TODO: make vr position in combination work properly with audio emitter/listener
                _vrSystem = OpenVR.Init(ref evrInitError, EVRApplicationType.VRApplication_Other);
                _isVrEnabled = evrInitError == EVRInitError.None;
                //_isVrEnabled = true;
            });

            _audioListener = new AudioListener();
        }

        private (Vector3 Position, Quaternion Rotation, Vector3 Velocity, Vector3 Omega) GetHmdPositioning()
        {
            //return (
            //    new Vector3((float)Math.Sin(sw.Elapsed.TotalSeconds * 3), 0, (float)Math.Sin(sw.Elapsed.TotalSeconds * 3)),
            //    new Quaternion(new Vector3(0, (float)Math.Sin(sw.Elapsed.TotalSeconds * 3), 0), 1),
            //    default,
            //    default);

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                var deviceClass = _vrSystem.GetTrackedDeviceClass(i);
                if (deviceClass != ETrackedDeviceClass.Invalid)
                {
                    var deviceReading = GetDeviceReading(i);

                    if (deviceClass == ETrackedDeviceClass.HMD)
                    {
                        //Console.WriteLine("OpenVR device at " + i + ": " + deviceClass + " and rotation " + deviceReading.GetRotation());
                        return deviceReading;
                    }
                }
            }

            return default;
        }

        private (Vector3 Position, Quaternion Rotatio, Vector3 Velocity, Vector3 Omega) GetDeviceReading(uint deviceId)
        {
            // this array can be reused
            TrackedDevicePose_t[] allPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

            _vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, allPoses);

            var pose = allPoses[deviceId];
            if (pose.bPoseIsValid)
            {
                var position = RigidTransform(pose.mDeviceToAbsoluteTracking);
                var velocity = new Vector3(pose.vVelocity.v0, pose.vVelocity.v1, pose.vVelocity.v2);
                var omega = new Vector3(pose.vAngularVelocity.v0, pose.vAngularVelocity.v1, pose.vAngularVelocity.v2);

                return (position.GetPosition(), position.GetRotation(), velocity, omega);
            }

            return default;
        }

        private Matrix4x4 RigidTransform(HmdMatrix34_t pose)
        {
            var matrix = new Matrix4x4
            {
                M11 = pose.m0,
                M12 = pose.m1,
                M13 = -pose.m2,
                M14 = pose.m3,

                M21 = pose.m4,
                M22 = pose.m5,
                M23 = -pose.m6,
                M24 = pose.m7,

                M31 = -pose.m8,
                M32 = -pose.m9,
                M33 = pose.m10,
                M34 = -pose.m11
            };

            //matrix[0, 0] = pose.m0;
            //matrix[0, 1] = pose.m1;
            //matrix[0, 2] = -pose.m2;
            //matrix[0, 3] = pose.m3;

            //matrix[1, 0] = pose.m4;
            //matrix[1, 1] = pose.m5;
            //matrix[1, 2] = -pose.m6;
            //matrix[1, 3] = pose.m7;

            //matrix[2, 0] = -pose.m8;
            //matrix[2, 1] = -pose.m9;
            //matrix[2, 2] = pose.m10;
            //matrix[2, 3] = -pose.m11;
            return matrix;
        }

        private CultureInfo GetLanguageFromText(string text)
        {
            if (_lazyLanguagesIdentifier == null)
            {
                var factory = new NaiveBayesLanguageIdentifierFactory();

                _lazyLanguagesIdentifier = factory.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream("wBeatSaberCamera.Ressource.LanguageProfile.xml"));
            }
            var res = _lazyLanguagesIdentifier.Identify(text);
            return GetCultureInfoFromIso3Name(res.FirstOrDefault()?.Item1.Iso639_3) ?? CultureInfo.InvariantCulture;
        }

        public static CultureInfo GetCultureInfoFromIso3Name(string name)
        {
            return CultureInfo
                   .GetCultures(CultureTypes.NeutralCultures)
                   .FirstOrDefault(c => c.ThreeLetterISOLanguageName == name);
        }

        public void Spek(string user, string text)
        {
            Func<Task> task = async () =>
            {
                var language = GetLanguageFromText(text);
                try
                {
                    if (_synthesizer == null)
                    {
                        _synthesizer = new SpeechSynthesizer();

                        //var voice = GetVoiceByName("IVONA 2 Justin OEM");
                        _synthesizer.Volume = 100; // 0...100
                        _synthesizer.Rate = 0; // -10...10

                        //_synthesizer.SelectVoice(voice.Name);
                    }

                    if (user == null || !Chatters.TryGetValue(user, out var chatter))
                    {
                        chatter = new Chatter()
                        {
                            Name = user
                        };

                        chatter.Position = /*Vector3.Right * 2;*/ (Vector3.Lerp(Vector3.Left, Vector3.Right, (float)s_random.NextDouble())
                                                                   + Vector3.Lerp(Vector3.Up, Vector3.Down, (float)s_random.NextDouble())
                                                                   + Vector3.Lerp(Vector3.Forward, Vector3.Backward, (float)s_random.NextDouble()));
                        chatter.Pitch = (s_random.NextDouble() * 2 - 1.0) * MaxPitchFactor;
                        chatter.TrembleBegin = s_random.NextDouble() * Math.PI * 2;
                        chatter.TrembleSpeed = s_random.NextDouble();
                        var factorMultMax = .3;
                        if (chatter.TrembleSpeed < .02)
                        {
                            factorMultMax = 2;
                        }

                        chatter.TrembleFactor = s_random.NextDouble() * factorMultMax;
                        if (user != null)
                        {
                            Chatters[user] = chatter;
                        }
                        //Console.WriteLine($"TS: {chatter.TrembleSpeed}, TF: {chatter.TrembleFactor}");
                    }

                    using (var ms = new MemoryStream())
                    {
                        chatter.WriteSpeechToStream(language, text, ms);
                        if (ms.Length == 0)
                        {
                            return;
                        }

                        ms.Position = 0;
                        var wavDuration = TimeSpan.FromMilliseconds(50);
                        try
                        {
                            wavDuration = new NAudio.Wave.WaveFileReader(ms).TotalTime - TimeSpan.FromMilliseconds(800);
                            if (wavDuration < TimeSpan.FromMilliseconds(50))
                            {
                                wavDuration = TimeSpan.FromMilliseconds(50);
                            }
                        }
                        catch
                        {
                            // meh
                        }

                        ms.Position = 0;
                        var soundEffect = SoundEffect.FromStream(ms).CreateInstance();
                        var audioEmitter = new AudioEmitter();
                        if (_isVrEnabled)
                        {
                            var hmdPositioning = GetHmdPositioning();
                            audioEmitter.Position = Vector3.Transform(chatter.Position, -hmdPositioning.Rotation);
                        }

                        soundEffect.Apply3D(_audioListener, audioEmitter);
                        soundEffect.Play();

                        double sineTime = chatter.TrembleBegin;
                        var stopWatch = Stopwatch.StartNew();
                        while (stopWatch.Elapsed < wavDuration)
                        {
                            sineTime += chatter.TrembleSpeed;
                            await Task.Delay(10);

                            if (_isVrEnabled)
                            {
                                var hmdPositioning = GetHmdPositioning();

                                var newAudioEmitterPosition = Vector3.Transform(chatter.Position, hmdPositioning.Rotation);
                                audioEmitter.Velocity = (newAudioEmitterPosition - audioEmitter.Position) * 100;
                                audioEmitter.Position = newAudioEmitterPosition;

                                _audioListener.Velocity = hmdPositioning.Velocity - audioEmitter.Position + Vector3.Transform(audioEmitter.Position, new Quaternion(hmdPositioning.Omega, 1));
                                //_audioListener.Position = hmdPositioning.Position;
                                //Console.WriteLine(audioEmitter.Position + "/" + _audioListener.Position);
                                soundEffect.Apply3D(_audioListener, audioEmitter);

                                //am.Rotation = position.GetRotation();
                            }

                            var pitch = chatter.Pitch + Math.Sin(sineTime) * chatter.TrembleFactor;
                            if (pitch < -1)
                            {
                                pitch = -1;
                            }

                            if (pitch > 1)
                            {
                                pitch = 1;
                            }

                            pitch *= MaxPitchFactor;

                            //Console.WriteLine(pitch);
                            soundEffect.Pitch = (float)pitch;
                        }
                    }

                    /*
                    try
                    {
                        _synthesizer.SpeakAsync(tbText.Text);
                    }
                    catch
                    {
                    }*/
                }
                catch (Exception ex)
                {
                    Log.Error($"Error while text text '{text}': " + ex);
                }
            };

            if (user != null)
            {
                _taskSerializer.Enqueue(user, task);
            }
            else
            {
                task();
            }
        }

        public void Spek(ChatMessage chatMessage)
        {
            if (!IsReadingStreamerMessagesEnabled && chatMessage.IsBroadcaster)
            {
                return;
            }
            Spek(chatMessage.Username, chatMessage.Message);
        }

        public override void Clean()
        {
            foreach (var chatter in Chatters.Values)
            {
                chatter.Clean();
            }
            base.Clean();
        }
    }

    //public static class sad
    //{
    //    public static Quaternion ToQuaternion(Vector3 v)
    //    {
    //        return ToQuaternion(v.Y, v.X, v.Z);
    //    }

    // public static Quaternion ToQuaternion(float yaw, float pitch, float roll) { yaw *=
    // Mathf.Deg2Rad; pitch *= Mathf.Deg2Rad; roll *= Mathf.Deg2Rad; float rollOver2 = roll * 0.5f;
    // float sinRollOver2 = (float)Math.Sin((double)rollOver2); float cosRollOver2 =
    // (float)Math.Cos((double)rollOver2); float pitchOver2 = pitch * 0.5f; float sinPitchOver2 =
    // (float)Math.Sin((double)pitchOver2); float cosPitchOver2 =
    // (float)Math.Cos((double)pitchOver2); float yawOver2 = yaw * 0.5f; float sinYawOver2 =
    // (float)Math.Sin((double)yawOver2); float cosYawOver2 = (float)Math.Cos((double)yawOver2);
    // Quaternion result; result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 *
    // sinPitchOver2 * sinRollOver2; result.x = cosYawOver2 * sinPitchOver2 * cosRollOver2 +
    // sinYawOver2 * cosPitchOver2 * sinRollOver2; result.y = sinYawOver2 * cosPitchOver2 *
    // cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2; result.z = cosYawOver2 *
    // cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;

    //        return result;
    //    }
    //}

    public static class Matrix4X4Extensions
    {
        public static Vector3 GetPosition(this Matrix4x4 matrix)
        {
            float x = matrix.M14;
            float y = matrix.M24;
            float z = matrix.M34;

            return new Vector3(x, y, z);
        }

        public static Quaternion GetRotation(this Matrix4x4 matrix)
        {
            var q = new Quaternion();
            q.X = (float)Math.Sqrt(Math.Max(0, 1f + matrix.M11 - matrix.M22 - matrix.M33)) / 2f;
            q.Y = (float)Math.Sqrt(Math.Max(0, 1f - matrix.M11 + matrix.M22 - matrix.M33)) / 2f;
            q.W = (float)Math.Sqrt(Math.Max(0, 1f + matrix.M11 + matrix.M22 + matrix.M33)) / 2f;
            q.Z = (float)Math.Sqrt(Math.Max(0, 1f - matrix.M11 - matrix.M22 + matrix.M33)) / 2f;
            q.X = CopySign(q.X, matrix.M32 - matrix.M23);
            q.Y = CopySign(q.Y, matrix.M13 - matrix.M31);
            q.Z = CopySign(q.Z, matrix.M21 - matrix.M12);
            return q;
        }

        private static float CopySign(float length, float signTester)
        {
            return Math.Sign(signTester) == 1 ? Math.Abs(length) : -Math.Abs(length);
        }
    }
}