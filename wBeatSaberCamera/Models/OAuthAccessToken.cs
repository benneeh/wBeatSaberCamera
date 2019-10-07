using System;
using System.Runtime.Serialization;
using wBeatSaberCamera.Utils;

namespace wBeatSaberCamera.Models
{
    [DataContract]
    public class OAuthAccessToken : DirtyBase
    {
        private string _accessToken;
        private DateTime _expiresAt;
        private int _userId;
        private string[] _scopes;
        private string _userName;
        private string _tokenType;

        [DataMember]
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                if (value?.StartsWith("oauth:") ?? false)
                {
                    value = value.Substring(6);
                }

                if (value == _accessToken)
                {
                    return;
                }

                _accessToken = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string TokenType
        {
            get => _tokenType;
            set
            {
                if (value == _tokenType)
                    return;

                _tokenType = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string UserName
        {
            get => _userName;
            set
            {
                if (value == _userName)
                    return;

                _userName = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string[] Scopes
        {
            get => _scopes;
            set
            {
                if (Equals(value, _scopes))
                    return;

                _scopes = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public int UserId
        {
            get => _userId;
            set
            {
                if (value == _userId)
                    return;

                _userId = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public DateTime ExpiresAt
        {
            get => _expiresAt;
            set
            {
                if (value.Equals(_expiresAt))
                    return;

                _expiresAt = value;
                OnPropertyChanged();
            }
        }
    }
}