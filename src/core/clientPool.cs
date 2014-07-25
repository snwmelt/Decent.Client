using System;
using System.IO;

namespace Decent.Client.core
{
    class clientPool
    {
        DirectoryInfo dirInfo;

        private clientPool (DirectoryInfo objectsDirectory, bool create)
        {
            if (!objectsDirectory.Exists)
            {
                if (create)
                {
                    objectsDirectory.Create();
                    this.objectsDirectory = objectsDirectory;
                }
                else
                {
                    throw new System.IO.IOException("Directory does not exist : " + objectsDirectory.FullName);
                }
            }
            else
            {
                this.objectsDirectory = objectsDirectory;
            }
        }

        public clientPool(DirectoryInfo objectsDirectory) : this(objectsDirectory, false) { }

        public clientPool(string objectsDirectory) : this(new DirectoryInfo(objectsDirectory), true) { }

        public DirectoryInfo objectsDirectory
        {
            private set
            {
                dirInfo = value;

                loadClientObejects();
            }

            get
            {
                return dirInfo;
            }
        }

        private void loadClientObejects()
        {
            throw new NotImplementedException();
        }
    }
}
