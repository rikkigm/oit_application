using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Octokit;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net.Mail;

namespace OIT_Application.Controllers
{
    public class MainController : Controller
    {
        //These would normally go in the config file. But is left here for easy review.
        private string token = "30dc7b7c2097a5511d8e57f075892c4d50950682";
        private string headerValue = "GetUsers";
        private string org = "rikkigmbyu";
        static string bucketName = "oitapplication";
        static string keyName = "githubUserList";
        static string awsAccessKeyID = "AKIAJMGOIU2XJI2GNRMA";
        static string awsSecretAccessKey = "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";
        //Place smtp server name here
        static string server = "";


        // GET: Main
        public ActionResult Index()
        {
            List<User> users = run();
            return View(users);
        }


        private List<User> run()
        {
            IReadOnlyList<User> users = GetGithubUsers();

            List<User> filteredUsers = FilterUsers(users);

            EmailUsers(filteredUsers);

            SaveListToAmazon(filteredUsers);

            return filteredUsers;

        }

        private void SaveListToAmazon(List<User> filteredUsers)
        {
            string listUsers = "";
            foreach (User user in filteredUsers)
            {
                listUsers += user.Login + ", " + user.Email + Environment.NewLine;
            }

            try
            {
                IAmazonS3 client;
                using (client = new AmazonS3Client(awsAccessKeyID, awsSecretAccessKey, Amazon.RegionEndpoint.USEast1))
                {
                    PutObjectRequest putRequest1 = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = keyName,
                        ContentBody = listUsers
                    };

                    PutObjectResponse response1 = client.PutObject(putRequest1);
                }
            }
            catch (Exception ex)
            {
                //Log the error if desired
            }
        }

        private void EmailUsers(List<User> filteredUsers)
        {
            foreach (User user in filteredUsers)
            {
                if (user.Email != null)
                {
                    //I don't have an smtp server, so this code is used from msdn and untested.
                    string to = user.Email;
                    string from = "rikkigmbyu@github.com";
                    MailMessage message = new MailMessage(from, to);
                    message.Subject = "Account Missing Name";
                    message.Body = @"Your Github account is missing a name. Press the following link to be able to add one https://github.com/settings/profile.";
                    SmtpClient client = new SmtpClient(server);
                    // Credentials are necessary if the server requires the client 
                    // to authenticate before it will send e-mail on the client's behalf.
                    client.UseDefaultCredentials = true;

                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception ex)
                    {
                       //Log the error if desired.
                    }
                }
            }
        }

        private List<User> FilterUsers(IReadOnlyList<User> users)
        {
            List<User> filteredUsers = new List<User>();

            foreach(User user in users)
            {
                if (user.Name == null)
                {
                    filteredUsers.Add(user);
                }
            }

            return filteredUsers;
        }

        private IReadOnlyList<User> GetGithubUsers()
        {
            Uri ghe = new Uri("https://github.com/org/rikkigmbyu");
            GitHubClient github = new GitHubClient(new ProductHeaderValue(headerValue), ghe);
            github.Credentials = new Credentials(token);
            IReadOnlyList<User> list = github.Organization.Member.GetAll(org).Result;

            return list;
        }
    }
}