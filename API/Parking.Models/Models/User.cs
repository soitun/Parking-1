﻿/**
 *
 * @package     Parking.API.Models
 * @author      The JSONs
 * @copyright   2012 - 20XX
 * @license     Propietary
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;
using System.Configuration;
using System.DirectoryServices;

namespace Sieena.Parking.API.Models
{
    using Views;
    using Interfaces;
    using Exceptions;
    using Sieena.Parking.Common.Utils;

    public partial class User : IUser
    {
        public static UserInformation GetUserInformation(User u)
        {
            UserInfo ui = UserInfo.GetByUserId(u.UserId);
            Role role = Role.GetRolesForUser(u.UserId).First();
            Session ses = Session.Get(u.UserId);

            var user = new UserInformation();

            user.Email = u.Email;
            user.UserName = u.Email.Split('@').First();
            user.IsAuthenticated = true;
            user.ProfilePictureUrl = "";
            user.FirstName = ui.FirstName;
            user.LastName = ui.LastName;
            user.Role = role.RoleName;
            user.RoleId = role.RoleId;

            user.SessionId = Crypto.EncryptStringAES(ses.SessionId.ToString(), ConfigurationManager.AppSettings["Crypto.Secret"]);

            return user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static User SaveUser(User u)
        {
            using (EntityContext ctx = new EntityContext())
            {
                u.Email = u.Email.ToLower();
                u.ValidateAndRaise();

                if (u.UserId == 0)
                {
                    u.Password = Crypto.GetSHA1(u.Password);
                    ctx.Users.AddObject(u);
                }
                else
                {
                    ctx.Users.Attach(u);
                }

                ctx.SaveChanges();

                return u;
            }
        }

        /// <summary>
        /// Adds roles to the user.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="r"></param>
        public static void AddRoles(User u, List<Role> r)
        {
            using (EntityContext ctx = new EntityContext())
            {
                foreach (Role ri in r)
                {
                    Models.UserRole ur = new Models.UserRole()
                    {
                        UserId = u.UserId,
                        RoleId = ri.RoleId
                    };
                    if (ctx.UserRoles.Where(uro => uro.UserId.Equals(u.UserId) && uro.RoleId.Equals(ri.RoleId)).Any())
                    {
                    }
                    else
                    {
                        ctx.UserRoles.AddObject(ur);
                    }
                }
                ctx.SaveChanges();
            }
        }

        /// <summary>
        /// Deletes the role.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="r"></param>
        public static void DeleteRoles(User u, List<Role> r)
        {
            using (EntityContext ctx = new EntityContext())
            {
                ctx.UserRoles.Where(ur => ur.UserId.Equals(u.UserId)
                                            && r.Where(ri => ri.RoleId.Equals(ur.RoleId)).Any()
                                        ).ToList().ForEach(ur => ctx.UserRoles.DeleteObject(ur));
                ctx.SaveChanges();
            }
        }

        /// <summary>
        /// Marks a user as deleted.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool DeleteUser(string email)
        {
            using (EntityContext ctx = new EntityContext())
            {
                User u = ctx.Users.Where(ux => ux.Email.Equals(email)).FirstOrDefault();
                if (u != null)
                {
                    u.IsActive = false;
                }

                ctx.SaveChanges();

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static User GetByEmail(string email)
        {
            using (EntityContext ctx = new EntityContext())
            {
                email = email.ToLower().Trim();
                return ctx.Users.Where(u => u.Email.Equals(email)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a user by its id
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static User GetById(int id)
        {
            using (EntityContext ctx = new EntityContext())
            {
                return ctx.Users.Where(u => u.UserId.Equals(id)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns all the users available in the system.
        /// </summary>
        /// <returns></returns>
        public static List<User> GetAll()
        {
            using (EntityContext ctx = new EntityContext())
            {
                return ctx.Users.ToList();
            }
        }

        /// <summary>
        /// Finds a set of users based on an email 
        /// </summary>
        /// <param name="emailToMatch"></param>
        /// <returns></returns>
        public static List<User> FindByEmail(string emailToMatch)
        {
            using (EntityContext ctx = new EntityContext())
            {
                emailToMatch = emailToMatch.ToLower().Trim();
                return ctx.Users.Where(u => u.Email.Contains(emailToMatch)).ToList();
            }
        }

        /// <summary>
        /// Verifies the credentials via active directory.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool VerifyCredentialsByAD(string user, string password)
        {
            user = user.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                // Throw custom buisness exception. 
                throw new APIException("User or password not specified");
            }

            string username = user.Split('@').First();
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, "SIEENA"))
                {
                    if (context.ValidateCredentials(username, password))
                    { 
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Could be several reasons, but go to next auth type 
            }

            return false;

        }

        /// <summary>
        /// Creates a user from AD
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static User CreateUserFromAD(string username, string password)
        {
            username = username.Split('@').First();
            
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, "SIEENA", "daniel.ramirez", "xrZ40uye"))
            {
                UserPrincipal up = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                using (EntityContext ctx = new EntityContext())
                {
                    User u = new User()
                    {
                        Email = up.EmailAddress,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        Password = password
                    };

                    ctx.Users.AddObject(u);
                    ctx.SaveChanges();


                    UserInfo ui = new UserInfo()
                    {
                        UserId = u.UserId,
                        FirstName = up.Name,
                        LastName = up.Surname
                    };

                    ctx.UserInfos.AddObject(ui);
                    ctx.SaveChanges();

                    User.AddRoles(u, new List<Role>() { Role.GetAll().OrderByDescending(r => r.RoleLevel).First() });

                    return u;
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool VerifyCredentials(string user, string password)
        {
            user = user.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                // Throw custom buisness exception. 
                throw new APIException("User or password not specified");
            }


            User u = GetByEmail(user);

            if (u == null)
            {
                if (VerifyCredentialsByAD(user, password))
                {
                    u = CreateUserFromAD(user, password);
                }
            }
            else if (u != null)
            {
                if (!u.Password.Equals(Crypto.GetSHA1(password)))
                {
                    u = null;
                }
            }

            return u != null;
        }

    }

}
