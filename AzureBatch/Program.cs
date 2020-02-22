using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureBatch
{
    class Program
    {

        // Add the batch account crdentials here
        private const string demo_batchAccountName = "demoacc2030";
        private const string demo_batchAccountKey = "rI/mdnln+GKsXpf8UKBeaYdFwVk8Tw6DNzljc4vqeeXqN2LHb7tS/ivGDWuH/6vPFXbO3kF3KJxoEHsrUgswKQ==";
        private const string demo_batchAccountUrl = "https://demoacc2030.centralus.batch.azure.com";

        // Here add the storage account details
        private const string demo_storageAccountName = "batchstore2020";
        private const string demo_storageAccountKey = "jehhy9QkFlhnY8tjFP8KwTIuvuXWanfi5RuAN9pbebYcmhKSKQd8buTAx0dMlynaVCkp3mifLQoVSmFhnpRnGQ==";

        // These are general values required for the batch service
        private const string PoolId = "demopool";
        private const string jobID = "video_processor";
        private const string demo_packageid = "ffmpeg";
        private const string demo_packageversion = "4.2";


        static void Main(string[] args)
        {
            try
            {
                CoreAsync().Wait();
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Program complete");
                Console.ReadLine();
            }
        }

        private static async Task CoreAsync()
        {
            BatchSharedKeyCredentials demo_sharedKeyCredentials = new BatchSharedKeyCredentials(demo_batchAccountUrl, demo_batchAccountName, demo_batchAccountKey);

            using (BatchClient demo_batchClient = BatchClient.Open(demo_sharedKeyCredentials))
            {

                // This method is used to create the pool
                await PoolCreation(demo_batchClient, PoolId);
                // This method is used to create the job
                await JobCreation(demo_batchClient, jobID, PoolId);
                // This method is used to create the task
                await TaskCreation(demo_batchClient, jobID);


            }

        }
        private static async Task PoolCreation(BatchClient p_batchClient, string p_poolId)
        {
            Console.WriteLine("Creating the pool of virtual machines");
            try
            {
                ImageReference demo_image = new ImageReference(
                            publisher: "MicrosoftWindowsServer",
                            offer: "WindowsServer",
                            sku: "2016-Datacenter",
                            version: "latest");

                VirtualMachineConfiguration demo_configuration =
                   new VirtualMachineConfiguration(
                       imageReference: demo_image,
                       nodeAgentSkuId: "batch.node.windows amd64");

                CloudPool demo_pool = null;

                demo_pool = p_batchClient.PoolOperations.CreatePool(
                        poolId: p_poolId,
                        targetDedicatedComputeNodes: 1,
                        targetLowPriorityComputeNodes: 0,
                        virtualMachineSize: "STANDARD_A1_v2",
                        virtualMachineConfiguration: demo_configuration);

                demo_pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                    ApplicationId = demo_packageid,
                    Version = demo_packageversion
                    }
                };

                await demo_pool.CommitAsync();
            }
            catch (BatchException pool_error)
            {
                Console.WriteLine(pool_error.Message);
            }
        }

        private static async Task JobCreation(BatchClient p_batchClient, string p_jobId, string p_poolId)
        {

            Console.WriteLine("Creating the job");

            CloudJob demo_job = p_batchClient.JobOperations.CreateJob();
            demo_job.Id = p_jobId;
            demo_job.PoolInformation = new PoolInformation { PoolId = p_poolId };

            await demo_job.CommitAsync();
        }

        private static async Task TaskCreation(BatchClient p_batchClient, string p_jobId)
        {

            Console.WriteLine("Creating the Task");

            string taskId = "demotask";
            string in_container_name = "input";
            string out_container_name = "output";
            string l_blobName = "sample.mp4";
            string outputfile = "audio.aac";


            string storageConnectionString = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                                demo_storageAccountName, demo_storageAccountKey);

            CloudStorageAccount l_storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            CloudBlobClient l_blobClient = l_storageAccount.CreateCloudBlobClient();


            CloudBlobContainer in_container = l_blobClient.GetContainerReference(in_container_name);
            CloudBlobContainer out_container = l_blobClient.GetContainerReference(out_container_name);

            SharedAccessBlobPolicy i_sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
            };

            SharedAccessBlobPolicy o_sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Write
            };



            string in_sasToken = in_container.GetSharedAccessSignature(i_sasConstraints);
            string in_containerSasUrl = String.Format("{0}{1}", in_container.Uri, in_sasToken);

            string out_sasToken = out_container.GetSharedAccessSignature(o_sasConstraints);
            string out_containerSasUrl = String.Format("{0}{1}", out_container.Uri, out_sasToken);


            ResourceFile inputFile = ResourceFile.FromStorageContainerUrl(in_containerSasUrl);

            List<ResourceFile> file = new List<ResourceFile>();
            file.Add(inputFile);

            string appPath = String.Format("%AZ_BATCH_APP_PACKAGE_{0}#{1}%", demo_packageid, demo_packageversion);

            string taskCommandLine = String.Format("cmd /c {0}\\ffmpeg.exe -i {1} -vn -acodec copy audio.aac", appPath, l_blobName);

            CloudTask task = new CloudTask(taskId, taskCommandLine);
            task.ResourceFiles = file;

            // Setting the output file location 
            List<OutputFile> outputFileList = new List<OutputFile>();
            OutputFileBlobContainerDestination outputContainer = new OutputFileBlobContainerDestination(out_containerSasUrl);
            OutputFile outputFile = new OutputFile(outputfile,
                                                   new OutputFileDestination(outputContainer),
                                                   new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));
            outputFileList.Add(outputFile);
            task.OutputFiles = outputFileList;


            await p_batchClient.JobOperations.AddTaskAsync(p_jobId, task);

        }

    }
}
