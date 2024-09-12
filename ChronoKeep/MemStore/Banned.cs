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
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedEmails.Add(email);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void AddBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "AddBannedEmails");
            database.AddBannedEmails(emails);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (string email in emails)
                    {
                        bannedEmails.Add(email);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void AddBannedPhone(string phone)
        {
            Log.D("MemStore", "AddBannedPhone");
            database.AddBannedPhone(phone);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedPhones.Add(phone);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void AddBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "AddBannedPhones");
            database.AddBannedPhones(phones);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (string phone in phones)
                    {
                        bannedPhones.Add(phone);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<string> GetBannedEmails()
        {
            Log.D("MemStore", "GetBannedEmails");
            List<string> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    output.AddRange(bannedEmails);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public List<string> GetBannedPhones()
        {
            Log.D("MemStore", "GetBannedPhones");
            List<string> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    output.AddRange(bannedPhones);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public void RemoveBannedEmail(string email)
        {
            Log.D("MemStore", "RemoveBannedEmail");
            database.RemoveBannedEmail(email);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedEmails.Remove(email);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "RemoveBannedEmails");
            database.RemoveBannedEmails(emails);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (string email in emails)
                    {
                        bannedEmails.Remove(email);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveBannedPhone(string phone)
        {
            Log.D("MemStore", "RemoveBannedPhone");
            database.RemoveBannedPhone(phone);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedPhones.Remove(phone);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "RemoveBannedPhones");
            database.RemoveBannedPhones(phones);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (string phone in phones)
                    {
                        bannedPhones.Remove(phone);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void ClearBannedEmails()
        {
            Log.D("MemStore", "ClearBannedEmails");
            database.ClearBannedEmails();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedEmails.Clear();
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void ClearBannedPhones()
        {
            Log.D("MemStore", "ClearBannedPhones");
            database.ClearBannedPhones();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    bannedPhones.Clear();
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }
    }
}
