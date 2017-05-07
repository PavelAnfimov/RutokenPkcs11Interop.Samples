﻿using System;
using System.Collections.Generic;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using RutokenPkcs11Interop;
using RutokenPkcs11Interop.Common;
using RutokenPkcs11Interop.Samples.Common;

namespace DeleteGOST28147_89
{
    class DeleteGOST28147_89
    {
        static readonly List<ObjectAttribute> SymmetricKeyAttributes = new List<ObjectAttribute>()
        {
            new ObjectAttribute(CKA.CKA_ID, SampleConstants.GostSecretKeyId),
            new ObjectAttribute(CKA.CKA_KEY_TYPE, (uint) Extended_CKK.CKK_GOST28147)
        };

        static void Main(string[] args)
        {
            try
            {
                // Инициализировать библиотеку
                Console.WriteLine("Library initialization");
                using (var pkcs11 = new Pkcs11(Settings.RutokenEcpDllDefaultPath, Settings.OsLockingDefault))
                {
                    // Получить доступный слот
                    Console.WriteLine("Checking tokens available");
                    Slot slot = Helpers.GetUsableSlot(pkcs11);

                    // Открыть RW сессию в первом доступном слоте
                    Console.WriteLine("Opening RW session");
                    using (Session session = slot.OpenSession(false))
                    {
                        // Выполнить аутентификацию Пользователя
                        Console.WriteLine("User authentication");
                        session.Login(CKU.CKU_USER, SampleConstants.NormalUserPin);

                        // Получить массив хэндлов объектов, соответствующих критериям поиска
                        Console.WriteLine("Getting secret keys...");
                        List<ObjectHandle> foundObjects = session.FindAllObjects(SymmetricKeyAttributes);

                        // Удалить ключи
                        if (foundObjects.Count > 0)
                        {
                            Console.WriteLine("Destroying objects...");
                            int objectsCounter = 1;
                            foreach (var foundObject in foundObjects)
                            {
                                Console.WriteLine($"   Object №{objectsCounter}");
                                session.DestroyObject(foundObject);
                                objectsCounter++;
                            }

                            Console.WriteLine("Objects have been destroyed successfully");
                        }
                        else
                        {
                            Console.WriteLine("No objects found");
                        }

                        // Сбросить права доступа
                        session.Logout();
                    }
                }
            }
            catch (Pkcs11Exception ex)
            {
                Console.WriteLine($"Operation failed [Method: {ex.Method}, RV: {ex.RV}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Operation failed [Message: {ex.Message}]");
            }
        }
    }
}
