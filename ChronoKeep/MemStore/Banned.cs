using Chronokeep.Helpers;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Banned Email/Phone Functions
         */

        public void AddBannedEmail(string email)
        {
            Log.D("MemStore", "AddBannedEmail");
            database.AddBannedEmail(email);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedEmails.Add(email);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void AddBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "AddBannedEmails");
            database.AddBannedEmails(emails);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (string email in emails)
                        {
                            bannedEmails.Add(email);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void AddBannedPhone(string phone)
        {
            Log.D("MemStore", "AddBannedPhone");
            database.AddBannedPhone(phone);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedPhones.Add(phone);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void AddBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "AddBannedPhones");
            database.AddBannedPhones(phones);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (string phone in phones)
                        {
                            bannedPhones.Add(phone);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public List<string> GetBannedEmails()
        {
            Log.D("MemStore", "GetBannedEmails");
            List<string> output = new();
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        output.AddRange(bannedEmails);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
            return output;
        }

        public List<string> GetBannedPhones()
        {
            Log.D("MemStore", "GetBannedPhones");
            List<string> output = new();
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        output.AddRange(bannedPhones);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
            return output;
        }

        public void RemoveBannedEmail(string email)
        {
            Log.D("MemStore", "RemoveBannedEmail");
            database.RemoveBannedEmail(email);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedEmails.Remove(email);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "RemoveBannedEmails");
            database.RemoveBannedEmails(emails);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (string email in emails)
                        {
                            bannedEmails.Remove(email);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveBannedPhone(string phone)
        {
            Log.D("MemStore", "RemoveBannedPhone");
            database.RemoveBannedPhone(phone);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedPhones.Remove(phone);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "RemoveBannedPhones");
            database.RemoveBannedPhones(phones);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (string phone in phones)
                        {
                            bannedPhones.Remove(phone);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void ClearBannedEmails()
        {
            Log.D("MemStore", "ClearBannedEmails");
            database.ClearBannedEmails();
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedEmails.Clear();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void ClearBannedPhones()
        {
            Log.D("MemStore", "ClearBannedPhones");
            database.ClearBannedPhones();
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        bannedPhones.Clear();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }
    }
}
