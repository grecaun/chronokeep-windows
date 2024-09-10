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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedEmails.Add(email);
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (string email in emails)
                    {
                        bannedEmails.Add(email);
                    }
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedPhones.Add(phone);
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (string phone in phones)
                    {
                        bannedPhones.Add(phone);
                    }
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    output.AddRange(bannedEmails);
                    memStoreLock.ExitReadLock();
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
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    output.AddRange(bannedPhones);
                    memStoreLock.ExitReadLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedEmails.Remove(email);
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (string email in emails)
                    {
                        bannedEmails.Remove(email);
                    }
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedPhones.Remove(phone);
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (string phone in phones)
                    {
                        bannedPhones.Remove(phone);
                    }
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedEmails.Clear();
                    memStoreLock.ExitWriteLock();
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
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    bannedPhones.Clear();
                    memStoreLock.ExitWriteLock();
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
