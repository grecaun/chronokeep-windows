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
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.AddBannedEmail(email);
                bannedEmails.Add(email);
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void AddBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "AddBannedEmails");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.AddBannedEmails(emails);
                foreach (string email in emails)
                {
                    bannedEmails.Add(email);
                }
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void AddBannedPhone(string phone)
        {
            Log.D("MemStore", "AddBannedPhone");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.AddBannedPhone(phone);
                bannedPhones.Add(phone);
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void AddBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "AddBannedPhones");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.AddBannedPhones(phones);
                foreach (string phone in phones)
                {
                    bannedPhones.Add(phone);
                }
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public List<string> GetBannedEmails()
        {
            Log.D("MemStore", "GetBannedEmails");
            List<string> output = new();
            try
            {
                bannedLock.AcquireReaderLock(lockTimeout);
                output.AddRange(bannedEmails);
                bannedLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
            return output;
        }

        public List<string> GetBannedPhones()
        {
            Log.D("MemStore", "GetBannedPhones");
            List<string> output = new();
            try
            {
                bannedLock.AcquireReaderLock(lockTimeout);
                output.AddRange(bannedPhones);
                bannedLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
            return output;
        }

        public void RemoveBannedEmail(string email)
        {
            Log.D("MemStore", "RemoveBannedEmail");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.RemoveBannedEmail(email);
                bannedEmails.Remove(email);
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void RemoveBannedEmails(List<string> emails)
        {
            Log.D("MemStore", "RemoveBannedEmails");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.RemoveBannedEmails(emails);
                foreach (string email in emails)
                {
                    bannedEmails.Remove(email);
                }
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void RemoveBannedPhone(string phone)
        {
            Log.D("MemStore", "RemoveBannedPhone");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.RemoveBannedPhone(phone);
                bannedPhones.Remove(phone);
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void RemoveBannedPhones(List<string> phones)
        {
            Log.D("MemStore", "RemoveBannedPhones");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.RemoveBannedPhones(phones);
                foreach (string phone in phones)
                {
                    bannedPhones.Remove(phone);
                }
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void ClearBannedEmails()
        {
            Log.D("MemStore", "ClearBannedEmails");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.ClearBannedEmails();
                bannedEmails.Clear();
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }

        public void ClearBannedPhones()
        {
            Log.D("MemStore", "ClearBannedPhones");
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                database.ClearBannedPhones();
                bannedPhones.Clear();
                bannedLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
        }
    }
}
