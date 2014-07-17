using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Decent.Client.core.objects
{
    [Serializable()]
    class client : IDisposable, ISerializable
    {
        private FileInfo  clientDO;
        private IPAddress IP;
        private string    key;
        private string    uname;

        // must check how to appropriately add non-serializable properties
        [NonSerialized]
        private FileStream FStream;
        [NonSerialized]
        int mwodio;

        public client (SerializationInfo info, StreamingContext context)
        {
            clientDiskObject = new FileInfo(info.GetString("clientDiskObjectPath"));
            clientIP         = new IPAddress((Byte[])info.GetValue("clientIPByteArry", Type.GetType("System.Byte[]")));
            clientKey        = info.GetString("clientKey");
            clientUsername   = info.GetString("clientUsername");
        }

        public client (FileInfo clientDiskObject, IPAddress clientIP, string clientKey, string clientUsername)
        {
            if (clientDiskObject == null || clientIP == null || String.IsNullOrEmpty(clientKey))
            {
                throw new System.ArgumentException("Missing Key Atribute");
            }

            this.clientDiskObject = clientDiskObject;
            this.clientIP         = clientIP;
            this.clientKey        = clientKey;
            this.clientUsername   = clientUsername;
        }

        public FileInfo clientDiskObject
        {
            set
            {
                if (value.Extension.Equals(".cdo"))
                {
                    if (!value.Exists)
                    {
                        try
                        {
                            value.Create().Dispose();

                            clientDO = value;
                        }
                        catch (IOException ioe)
                        {
                            Debug.WriteLine(ioe);

                            this.Dispose();
                        }
                    }
                    else
                    {
                        clientDO = value;
                    }
                }
                else
                {
                    throw new System.ArgumentException("Invalid File Type");
                }
            }

            get
            {
                return clientDO;
            }
        }

        public IPAddress clientIP 
        { 
            set
            {
                IP = value;
            }
            
            get
            {
                return IP;
            }
        }

        public string clientKey
        {
            set
            {
                key = value;
            }

            get
            {
                return key;
            }
        }

        public string clientUsername 
        {
            set
            {
                uname = value;
            }

            get 
            {
                return uname;
            }
        }

        public static client Deserialize(FileInfo clientDiskObject, int maxWaitOnDiskIO)
        {
            if (maxWaitOnDiskIO < 0 || 60 < maxWaitOnDiskIO)
                throw new System.ArgumentException("Input Out Wait Seconds Out Of Bounds : " + maxWaitOnDiskIO);

            if (!clientDiskObject.Exists) 
                throw new System.ArgumentException("Client Object Does Not Exist \n FileInfo.FullName : " + clientDiskObject.FullName);

            try
            {
                using (FileStream FStream = new FileStream(clientDiskObject.FullName, FileMode.Open))
                {
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    client client              = (client)BFormatter.Deserialize(FStream);
                    client.clientDiskObject    = clientDiskObject;

                    return client;
                }
            }
            catch (IOException ioe)
            {
                Debug.WriteLine(ioe);

                DateTime maxWait = (DateTime.Now + TimeSpan.FromSeconds(10));

                while (true)
                {
                    if (DateTime.Now.Second.Equals(maxWait.Second)) break;
                    
                    try
                    {
                        using (FileStream FStream = new FileStream(clientDiskObject.FullName, FileMode.Open))
                        {
                            BinaryFormatter BFormatter = new BinaryFormatter();
                            client client              = (client)BFormatter.Deserialize(FStream);
                            client.clientDiskObject    = clientDiskObject;

                            return client;
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch (SerializationException se)
            {
                Debug.WriteLine(se);

                clientDiskObject.Delete();

                return null;
            }
        }

        public void Dispose()
        {
            if (FStream != null)
                FStream.Dispose();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("clientDiskObjectPath", clientDiskObject.FullName);
            info.AddValue("clientIPByteArry",     clientIP.GetAddressBytes());
            info.AddValue("clientKey",            clientKey);
            info.AddValue("clientUsername",       clientUsername);
        }

        private int maxWaitOnDiskIO
        {
            set
            {
                if (value < 0 || 60 < value)
                    throw new System.ArgumentException("Input Out Of Bounds");

                mwodio = value;
            }

            get
            {
                return mwodio;
            }
        }

        private void openFileStream()
        {
            try
            {
                FStream = new FileStream(clientDiskObject.FullName, FileMode.OpenOrCreate);
            }
            catch (IOException ioe)
            {
                Debug.WriteLine(ioe);

                DateTime maxWait = (DateTime.Now + TimeSpan.FromSeconds(maxWaitOnDiskIO));

                while (true)
                {
                    if (DateTime.Now.Second.Equals(maxWait.Second)) break;

                    try
                    {
                        FStream = new FileStream(clientDiskObject.FullName, FileMode.OpenOrCreate);
                    }
                    catch { }
                }

                FStream = null;
            }
        }

        public void Serialize(int maxWaitOnDiskIO)
        {
            lock (this)
            {
                this.maxWaitOnDiskIO = maxWaitOnDiskIO;

                if (FStream == null) openFileStream();

                if (FStream != null)
                {
                    BinaryFormatter BFormatter = new BinaryFormatter();
                    BFormatter.Serialize(FStream, this);

                    FStream.Dispose();
                }
            }
        }
    }
}
