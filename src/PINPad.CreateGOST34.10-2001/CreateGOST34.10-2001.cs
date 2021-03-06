﻿using System;
using System.Collections.Generic;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using RutokenPkcs11Interop;
using RutokenPkcs11Interop.Common;
using RutokenPkcs11Interop.HighLevelAPI;
using RutokenPkcs11Interop.Samples.Common;

namespace PINPad.CreateGOST3410_2001
{
    /*************************************************************************
    * Rutoken                                                                *
    * Copyright (c) 2003-2019, CJSC Aktiv-Soft. All rights reserved.         *
    * Подробная информация:  http://www.rutoken.ru                           *
    *------------------------------------------------------------------------*
    * Пример работы с Рутокен PINPad при помощи библиотеки PKCS#11           *
    * на языке C#                                                            *
    *------------------------------------------------------------------------*
    * Использование команд создания объектов в памяти Рутокен PINPad:        *
    *  - установление соединения с Рутокен PINPad в первом доступном слоте;  *
    *  - определение модели подключенного устройства;                        *
    *  - выполнение аутентификации Пользователя;                             *
    *  - создание ключевой пары ГОСТ Р 34.10-2001 с атрибутами подтверждения *
    *    подписи данных и вводом PIN-кода на экране PINPad;                  *
    *  - сброс прав доступа Пользователя на Рутокен PINPad и закрытие        *
    *    соединения с Рутокен PINPad.                                        *
    *------------------------------------------------------------------------*
    * Созданные примером объекты используются также и в других примерах      *
    * работы с библиотекой PKCS#11.                                          *
    *************************************************************************/

    class CreateGOST3410_2001
    {
        // Шаблон для создания открытого ключа ГОСТ Р 34.10-2001
        static readonly List<ObjectAttribute> PublicKeyAttributes = new List<ObjectAttribute>
        {
            // Объект открытого ключа
            new ObjectAttribute(CKA.CKA_CLASS, CKO.CKO_PUBLIC_KEY),
            // Метка ключа
            new ObjectAttribute(CKA.CKA_LABEL, SampleConstants.GostPublicKeyLabel1),
            // Идентификатор ключевой пары (должен совпадать у открытого и закрытого ключей)
            new ObjectAttribute(CKA.CKA_ID, SampleConstants.GostKeyPairId1),
            // Тип ключа ГОСТ Р 34.10-2001
            new ObjectAttribute(CKA.CKA_KEY_TYPE, (uint)Extended_CKK.CKK_GOSTR3410),
            // Ключ является объектом токена
            new ObjectAttribute(CKA.CKA_TOKEN, true),
            // Ключ доступен без аутентификации
            new ObjectAttribute(CKA.CKA_PRIVATE, false),
            // Параметры алгоритма ГОСТ Р 34.10-2001
            new ObjectAttribute((uint) Extended_CKA.CKA_GOSTR3410_PARAMS, SampleConstants.GostR3410Parameters),
            // Параметры алгоритма ГОСТ Р 34.10-2001
            new ObjectAttribute((uint) Extended_CKA.CKA_GOSTR3411_PARAMS, SampleConstants.GostR3411Parameters)
        };

        // Шаблон для создания закрытого ключа ГОСТ Р 34.10-2001
        static readonly List<ObjectAttribute> PrivateKeyAttributes = new List<ObjectAttribute>
        {
            // Объект закрытого ключа
            new ObjectAttribute(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
            // Метка ключа
            new ObjectAttribute(CKA.CKA_LABEL, SampleConstants.GostPrivateKeyLabel1),
            // Идентификатор ключевой пары (должен совпадать у открытого и закрытого ключей)
            new ObjectAttribute(CKA.CKA_ID, SampleConstants.GostKeyPairId1),
            // Тип ключа ГОСТ Р 34.10-2001
            new ObjectAttribute(CKA.CKA_KEY_TYPE, (uint)Extended_CKK.CKK_GOSTR3410),
             // Ключ является объектом токена
            new ObjectAttribute(CKA.CKA_TOKEN, true),
            // Ключ доступен только после аутентификации
            new ObjectAttribute(CKA.CKA_PRIVATE, true),
             // Операция подписи требует подтверждения на PINPad
            new ObjectAttribute((uint) Extended_CKA.CKA_VENDOR_KEY_CONFIRM_OP, true),
            // Операция подписи требует ввода PIN-кода на PINPad
            new ObjectAttribute((uint) Extended_CKA.CKA_VENDOR_KEY_PIN_ENTER, true),
            // Параметры алгоритма ГОСТ Р 34.10-2001
            new ObjectAttribute((uint) Extended_CKA.CKA_GOSTR3410_PARAMS, SampleConstants.GostR3410Parameters),
            // Параметры алгоритма ГОСТ Р 34.11-1994
            new ObjectAttribute((uint) Extended_CKA.CKA_GOSTR3411_PARAMS, SampleConstants.GostR3411Parameters)
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

                    // Получить расширенную информацию о подключенном токене
                    Console.WriteLine("Checking token type");
                    TokenInfoExtended tokenInfo = slot.GetTokenInfoExtended();
                    // Проверить наличие PINPad в нулевом слоте
                    Errors.Check("Device in slot 0 is not Rutoken PINPad", tokenInfo.TokenType == RutokenType.PINPAD_FAMILY);

                    // Определение поддерживаемых токеном механизмов
                    Console.WriteLine("Checking mechanisms available");
                    List<CKM> mechanisms = slot.GetMechanismList();
                    Errors.Check(" No mechanisms available", mechanisms.Count > 0);
                    bool isGostR3410Supported = mechanisms.Contains((CKM)Extended_CKM.CKM_GOSTR3410_KEY_PAIR_GEN);
                    Errors.Check(" CKM_GOSTR3410_KEY_PAIR_GEN isn`t supported!", isGostR3410Supported);

                    // Открыть RW сессию в первом доступном слоте
                    Console.WriteLine("Opening RW session");
                    using (Session session = slot.OpenSession(false))
                    {
                        // Выполнить аутентификацию Пользователя
                        Console.WriteLine("User authentication");
                        session.Login(CKU.CKU_USER, SampleConstants.NormalUserPin);

                        try
                        {
                            // Определить механизм генерации ключа
                            Console.WriteLine("Generating GOST R 34.10-2001 key pair...");
                            var mechanism = new Mechanism((uint)Extended_CKM.CKM_GOSTR3410_KEY_PAIR_GEN);

                            // Сгенерировать первую ключевую пару ГОСТ Р 34.10-2001
                            ObjectHandle publicKeyHandle;
                            ObjectHandle privateKeyHandle;
                            session.GenerateKeyPair(mechanism, PublicKeyAttributes, PrivateKeyAttributes, out publicKeyHandle, out privateKeyHandle);
                            Errors.Check("Invalid public key handle", publicKeyHandle.ObjectId != CK.CK_INVALID_HANDLE);
                            Errors.Check("Invalid private key handle", privateKeyHandle.ObjectId != CK.CK_INVALID_HANDLE);

                            Console.WriteLine("Generating has been completed successfully");
                        }
                        finally
                        {
                            // Сбросить права доступа как в случае исключения,
                            // так и в случае успеха.
                            // Сессия закрывается автоматически.
                            session.Logout();
                        }
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
