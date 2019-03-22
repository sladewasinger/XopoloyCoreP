/*!
 * ASP.NET SignalR JavaScript Library v2.3.0-rtm
 * http://signalr.net/
 *
 * Copyright (c) .NET Foundation. All rights reserved.
 * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
 *
 */

/// <reference path="..\..\SignalR.Client.JS\Scripts\jquery-1.6.4.js" />
/// <reference path="jquery.signalR.js" />
(function ($, window, undefined) {
    /// <param name="$" type="jQuery" />
    "use strict";

    if (typeof ($.signalR) !== "function") {
        throw new Error("SignalR: SignalR is not loaded. Please ensure jquery.signalR-x.js is referenced before ~/signalr/js.");
    }

    var signalR = $.signalR;

    function makeProxyCallback(hub, callback) {
        return function () {
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    }

    function registerHubProxies(instance, shouldSubscribe) {
        var key, hub, memberKey, memberValue, subscriptionMethod;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                hub = instance[key];

                if (!(hub.hubName)) {
                    // Not a client hub
                    continue;
                }

                if (shouldSubscribe) {
                    // We want to subscribe to the hub events
                    subscriptionMethod = hub.on;
                } else {
                    // We want to unsubscribe from the hub events
                    subscriptionMethod = hub.off;
                }

                // Loop through all members on the hub and find client hub functions to subscribe/unsubscribe
                for (memberKey in hub.client) {
                    if (hub.client.hasOwnProperty(memberKey)) {
                        memberValue = hub.client[memberKey];

                        if (!$.isFunction(memberValue)) {
                            // Not a client hub function
                            continue;
                        }

                        // Use the actual user-provided callback as the "identity" value for the registration.
                        subscriptionMethod.call(hub, memberKey, makeProxyCallback(hub, memberValue), memberValue);
                    }
                }
            }
        }
    }

    $.hubConnection.prototype.createHubProxies = function () {
        var proxies = {};
        this.starting(function () {
            // Register the hub proxies as subscribed
            // (instance, shouldSubscribe)
            registerHubProxies(proxies, true);

            this._registerSubscribedHubs();
        }).disconnected(function () {
            // Unsubscribe all hub proxies when we "disconnect".  This is to ensure that we do not re-add functional call backs.
            // (instance, shouldSubscribe)
            registerHubProxies(proxies, false);
        });

        proxies['lobbyHub'] = this.createHubProxy('lobbyHub'); 
        proxies['lobbyHub'].client = { };
        proxies['lobbyHub'].server = {
            acceptTrade: function (tradeOfferID) {
            /// <summary>Calls the AcceptTrade method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tradeOfferID\" type=\"Object\">Server side type is System.Guid</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["AcceptTrade"], $.makeArray(arguments)));
             },

            betOnAuction: function (amount) {
            /// <summary>Calls the BetOnAuction method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"amount\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["BetOnAuction"], $.makeArray(arguments)));
             },

            buildHouse: function (tileIndex) {
            /// <summary>Calls the BuildHouse method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tileIndex\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["BuildHouse"], $.makeArray(arguments)));
             },

            buyOutOfJail: function () {
            /// <summary>Calls the BuyOutOfJail method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["BuyOutOfJail"], $.makeArray(arguments)));
             },

            buyProperty: function () {
            /// <summary>Calls the BuyProperty method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["BuyProperty"], $.makeArray(arguments)));
             },

            createLobby: function (lobbyName, isPublic) {
            /// <summary>Calls the CreateLobby method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"lobbyName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"isPublic\" type=\"\">Server side type is System.Boolean</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["CreateLobby"], $.makeArray(arguments)));
             },

            declareBankruptcy: function () {
            /// <summary>Calls the DeclareBankruptcy method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["DeclareBankruptcy"], $.makeArray(arguments)));
             },

            disconnectFromLobby: function () {
            /// <summary>Calls the DisconnectFromLobby method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["DisconnectFromLobby"], $.makeArray(arguments)));
             },

            disconnectPlayer: function () {
            /// <summary>Calls the DisconnectPlayer method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["DisconnectPlayer"], $.makeArray(arguments)));
             },

            endTurn: function () {
            /// <summary>Calls the EndTurn method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["EndTurn"], $.makeArray(arguments)));
             },

            instantMonopoly: function () {
            /// <summary>Calls the InstantMonopoly method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["InstantMonopoly"], $.makeArray(arguments)));
             },

            joinLobby: function (lobbyID) {
            /// <summary>Calls the JoinLobby method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"lobbyID\" type=\"String\">Server side type is System.String</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["JoinLobby"], $.makeArray(arguments)));
             },

            mortgageProperty: function (tileIndex) {
            /// <summary>Calls the MortgageProperty method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tileIndex\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["MortgageProperty"], $.makeArray(arguments)));
             },

            offerTrade: function (targetPlayerID, tileIndexesA, tileIndexesB, moneyA, moneyB) {
            /// <summary>Calls the OfferTrade method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"targetPlayerID\" type=\"Object\">Server side type is System.Guid</param>
            /// <param name=\"tileIndexesA\" type=\"Object\">Server side type is System.Collections.Generic.List`1[System.Int32]</param>
            /// <param name=\"tileIndexesB\" type=\"Object\">Server side type is System.Collections.Generic.List`1[System.Int32]</param>
            /// <param name=\"moneyA\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"moneyB\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["OfferTrade"], $.makeArray(arguments)));
             },

            redeemProperty: function (tileIndex) {
            /// <summary>Calls the RedeemProperty method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tileIndex\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["RedeemProperty"], $.makeArray(arguments)));
             },

            registerPlayer: function (username) {
            /// <summary>Calls the RegisterPlayer method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"username\" type=\"String\">Server side type is System.String</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["RegisterPlayer"], $.makeArray(arguments)));
             },

            rejectTrade: function (tradeOfferID) {
            /// <summary>Calls the RejectTrade method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tradeOfferID\" type=\"Object\">Server side type is System.Guid</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["RejectTrade"], $.makeArray(arguments)));
             },

            requestStateUpdate: function () {
            /// <summary>Calls the RequestStateUpdate method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["RequestStateUpdate"], $.makeArray(arguments)));
             },

            rollDice: function () {
            /// <summary>Calls the RollDice method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["RollDice"], $.makeArray(arguments)));
             },

            sellHouse: function (tileIndex) {
            /// <summary>Calls the SellHouse method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"tileIndex\" type=\"Number\">Server side type is System.Int32</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["SellHouse"], $.makeArray(arguments)));
             },

            spectateLobby: function (lobbyID) {
            /// <summary>Calls the SpectateLobby method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"lobbyID\" type=\"String\">Server side type is System.String</param>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["SpectateLobby"], $.makeArray(arguments)));
             },

            startAuctionOnProperty: function () {
            /// <summary>Calls the StartAuctionOnProperty method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["StartAuctionOnProperty"], $.makeArray(arguments)));
             },

            startGame: function () {
            /// <summary>Calls the StartGame method on the server-side LobbyHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies['lobbyHub'].invoke.apply(proxies['lobbyHub'], $.merge(["StartGame"], $.makeArray(arguments)));
             }
        };

        return proxies;
    };

    signalR.hub = $.hubConnection("/signalr", { useDefaultPath: false });
    $.extend(signalR, signalR.hub.createHubProxies());

}(window.jQuery, window));