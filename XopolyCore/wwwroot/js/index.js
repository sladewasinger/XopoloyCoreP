Array.prototype.groupBy = function (prop) {
    return this.reduce(function (groups, item) {
        const val = item[prop]
        groups[val] = groups[val] || []
        groups[val].push(item)
        return groups
    }, {})
};

function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

var re = /\[([a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12})\]/i;
function extractGuid(value) {

    // the RegEx will match the first occurrence of the pattern
    var match = re.exec(value);

    // result is an array containing:
    // [0] the entire string that was matched by our RegEx
    // [1] the first (only) group within our match, specified by the
    // () within our pattern, which contains the GUID value

    return match ? match[1] : null;
}

$(function () {
    //====================//
    //Vue.js setup:
    let vueApp = new Vue({
        el: '#server_state',
        data: {
            state: {
                players: [],
                lobbies: []
            },
            gameState: {
                players: [],
                tiles: []
            },
            gameStateQueue: [],
            prevGameState: {
                players: [],
                tiles: []
            },
            username: null,
            lobbyName: null,
            lobbyID_txt: null,
            playerID: null,
            lobbyIsPublic: true,
            privateLobby: null,
            gameLog: "",
            betAmount_txt: null,
            gameID: null,
            propertySelectionInProgress: false,
            propertySelected: null,
            prevPlayerTurnID: null,
            prevPlayerBoardPosition: null,
            tileClickAction: function () { },
            propertySelectionType: "none",
            lastDiceRollInternal: [6, 1],
            createTradeInProgress: false,
            tradeTargetPlayerID: null,
            selectedTradeTargetPlayerProperties: [],
            selectedTradeTargetPlayerMoney: 0,
            selectedTradeMyPlayerProperties: [],
            selectedTradeMyPlayerMoney: 0,
            currentUIBoardPositions: [],
            prevUIBoardPositions: [],
            animationInProgress: false,
            animationTimeoutHandle: null,
            eventPopupQueue: [],
            lobbyBarCollapsed: false,
            localStorageName: 'XOPOLYSTORAGE',
            showTradeOffersEnabled: false,
            infoTile: null,
            soundEnabled: true,
            processingGameState: false
        },
        computed: {
            isPlayerRegistered: function () {
                return this.playerID && this.state.players && this.state.players.some(p => p.computerUserID == this.playerID);
            },
            isPlayerInLobby: function () {
                return this.state.lobbies.some(lobby => lobby.players.some(p => p.computerUserID == this.playerID));
            },
            playerOwnsALobby: function () {
                return this.state.lobbies.some(lobby => lobby.owner.computerUserID == this.playerID);
            },
            gamePlayer: function () {
                let myPlayerList = this.gameState.players.filter(p => p.id == this.gameID);
                if (myPlayerList && myPlayerList.length > 0)
                    return myPlayerList[0];
                else
                    return null;
            },
            ownedProperties: function () {
                let ownedProps = [];
                if (this.gameID != null && this.gameState.tiles)
                    ownedProps = this.gameState.tiles.filter(t => t.ownerPlayerID == this.gameID)
                return ownedProps;
            },
            gameInProgress: function () {
                return this.isPlayerInLobby && this.gameState && this.gameState.players && this.gameState.players.length;
            },
            isPlayersTurn: function () {
                return this.gameInProgress &&
                    this.gameState.currentPlayer.id == this.gameID &&
                    !this.animationInProgress;
            },
            canEndTurn: function () {
                let curPlayer = this.gameState.currentPlayer;
                return this.isPlayersTurn &&
                    curPlayer.currentDiceRoll &&
                    (!curPlayer.currentDiceRoll.isDouble || curPlayer.isInJail) &&
                    !this.gameState.waitForBuyOrAuctionStart;
            },
            canBuy: function () {
                return this.isPlayersTurn &&
                    !this.propertySelectionInProgress &&
                    this.gameState.waitForBuyOrAuctionStart &&
                    this.gameState.currentPlayer.money >= this.gameState.currentTile.cost;
            },
            canAuction: function () {
                return this.isPlayersTurn &&
                    !this.propertySelectionInProgress &&
                    this.gameState.waitForBuyOrAuctionStart;
            },
            canBetOnAuction: function () {
                return this.gameState.auction &&
                    this.gamePlayer.money > 0 &&
                    !this.gameState.auction.auctionParticipants.filter(x => x.id == this.gameID)[0].hasPlacedBet;
            },
            canPayJailFee: function () {
                return this.isPlayersTurn &&
                    !this.propertySelectionInProgress &&
                    this.gameState.currentPlayer.isInJail &&
                    !this.gameState.currentPlayer.currentDiceRoll;
            },
            canUseGOOJFC: function () {
                return this.isPlayersTurn &&
                    this.gamePlayer.isInJail &&
                    this.gamePlayer.hasGetOutOfJailFreeCard;
            },
            canBuild: function () {
                var canBuild = false;

                if (this.isPlayersTurn && this.ownedProperties.length > 1) {
                    let coloredProperties = this.gameState.tiles.filter(x => x.type == 'ColorProperty').groupBy('color');
                    canBuild = Object.keys(coloredProperties).some(key =>
                        coloredProperties[key].every(prop => prop.ownerPlayerID == this.gameID) &&
                        coloredProperties[key].every(prop => !prop.isMortgaged) &&
                        coloredProperties[key].some(prop => prop.buildingCount < 5));
                }

                canBuild = canBuild || (this.propertySelectionInProgress && this.propertySelectionType == "BuildHouse");

                return canBuild;
            },
            canSell: function () {
                var canSell = false;

                if (this.isPlayersTurn && this.ownedProperties.length > 1) {
                    let coloredProperties = this.gameState.tiles.filter(x => x.type == 'ColorProperty').groupBy('color');
                    canSell = Object.keys(coloredProperties).some(key =>
                        coloredProperties[key].some(prop => prop.buildingCount > 0) &&
                        coloredProperties[key].every(prop => prop.ownerPlayerID == this.gameID));
                }

                canSell = canSell || (this.propertySelectionInProgress && this.propertySelectionType == "SellHouse");

                return canSell;
            },
            canBankruptcy: function () {
                return this.isPlayersTurn
                    && this.gameState.currentPlayer.money < 0;
            },
            canRollDice: function () {
                return this.isPlayersTurn &&
                    !this.propertySelectionInProgress &&
                    !this.gameState.waitForBuyOrAuctionStart &&
                    this.gameState.currentPlayer.money >= 0 &&
                    (!this.gameState.currentPlayer.currentDiceRoll ||
                        (this.gameState.currentPlayer.currentDiceRoll.isDouble && !this.gameState.currentPlayer.isInJail));
            },
            canMortgageProperty: function () {
                var canMortgage = false;

                canMortgage = this.isPlayersTurn &&
                    this.ownedProperties.length > 0 &&
                    this.ownedProperties.some(x => !x.isMortgaged);

                let coloredPropertyGroups = this.gameState.tiles.filter(x => x.type == 'ColorProperty').groupBy('color');
                let ownedMonopolyColors = Object.keys(coloredPropertyGroups).filter(key => coloredPropertyGroups[key].every(prop => prop.ownerPlayerID == this.gameID));
                let ownedMonopolies = ownedMonopolyColors.map(k => coloredPropertyGroups[k]);
                if (ownedMonopolies &&
                    ownedMonopolies.length > 0 &&
                    ownedMonopolies.every(monopolyProps => monopolyProps.some(prop => prop.buildingCount > 0) || monopolyProps.every(prop => prop.isMortgaged)) &&
                    !this.ownedProperties
                        .filter(x => !ownedMonopolies.some(monopolyProps => monopolyProps.some(prop => prop.id == x.id)))
                        .some(prop => !prop.isMortgaged)) {
                    canMortgage = false;
                }

                canMortgage = canMortgage || (this.propertySelectionInProgress && this.propertySelectionType == "MortgageProperty");

                return canMortgage;
            },
            canRedeemProperty: function () {
                return this.isPlayersTurn &&
                    this.ownedProperties.length > 0 &&
                    (this.ownedProperties.some(x => x.isMortgaged) || (this.propertySelectionInProgress && this.propertySelectionType == "RedeemProperty"));
            },
            canCreateTrade: function () {
                return !this.createTradeInProgress &&
                    this.otherPlayers && this.otherPlayers.length > 0;
            },
            lastDiceRoll: function () {
                if (this.gameState.currentPlayer.currentDiceRoll) {
                    this.lastDiceRollInternal = this.gameState.currentPlayer.currentDiceRoll.dice;
                } else if (this.gameState.players.some(x => x.currentDiceRoll && x.currentDiceRoll.dice)) {
                    this.lastDiceRollInternal = this.gameState.players.filter(x => x.currentDiceRoll && x.currentDiceRoll.dice)[0].currentDiceRoll.dice;
                }

                return this.lastDiceRollInternal;
            },
            otherPlayers: function () {
                let _gameID = this.gameID;
                return this.gameState.players.filter(x => x.id != _gameID);
            },
            isStageTwoAction: function () {
                return this.canBuy || this.canAuction || this.canBetOnAuction || this.gameState.auction;
            },
            playerHasTradeOffers: function () {
                return this.gameState.tradeOffers.some(x => x.playerB.id == this.gameID);
            },
            playersTradeOffers: function () {
                return this.gameState.tradeOffers.filter(x => x.playerB.id == this.gameID);
            }
        },
        methods: {
            enableShowTradeOffers: function () {
                this.showTradeOffersEnabled = true;
            },
            disableShowTradeOffers: function () {
                this.showTradeOffersEnabled = false;
            },
            playerHasExistingTradeOfferWith: function (playerB_ID) {
                return this.gameState.tradeOffers.some(x => x.playerB.id == playerB_ID && x.playerA.id == this.gameID);
            },
            disconnectFromHub: function () {
                $.connection.hub.stop();
            },
            setLocalStorageData: function (localData) {
                localStorage.setItem(this.localStorageName, JSON.stringify(localData));
            },
            getLocalStorageData: function () {
                let localData = {};
                let localDataRaw = localStorage.getItem(this.localStorageName);
                localData = JSON.parse(localDataRaw) || localData;

                return localData;
            },
            setLocalStorageDataItem: function (key, value) {
                let localData = this.getLocalStorageData();
                localData[key] = value;
                this.setLocalStorageData(localData);
            },
            registerPlayer: function () {
                if (!this.username) {
                    return;
                }
                console.log("Attempting to register player", this.username);
                connection.invoke("registerPlayer", this.username).then(() => {
                    this.setLocalStorageDataItem('username', this.username);
                }).catch(function (error) {
                    console.log("register player failed: ", error);
                });
            },
            createLobby: function () {
                connection.invoke("createLobby", this.lobbyName, this.lobbyIsPublic);
            },
            refreshState: function () {
                connection.invoke("requestStateUpdate");
            },
            disconnectFromLobby: function () {
                this.lobbyID_txt = null;
                this.lobbyName = null;
                this.gameState = {
                    players: [],
                    tiles: []
                };
                connection.invoke("disconnectFromLobby");
            },
            joinLobby: function (lobbyID) {
                connection.invoke("joinLobby", lobbyID);
            },
            spectateLobby: function (lobbyID) {
                connection.invoke("spectateLobby", lobbyID);
            },
            updatePlayer: function (player) {
                this.playerID = player.computerUserID;
                this.username = player.username;
                this.gameID = player.gameID;
            },
            updatePrivateLobby: function (privateLobby) {
                this.privateLobby = privateLobby;
            },
            disconnectPlayer: function () {
                console.log("disconnecting player");
                this.lobbyID_txt = null;
                this.lobbyName = null;
                this.gameState = {
                    players: [],
                    tiles: []
                };
                this.state = {
                    players: [],
                    lobbies: []
                };
                this.playerID = null;
                this.username = null;
                connection.invoke("disconnectPlayer");
            },
            startGame: function () {
                connection.invoke("startGame");
                this.lobbyBarCollapsed = true;
            },
            rollDice: function () {
                connection.invoke("rollDice");
            },
            updateGameLog: function (gameLog) {
                this.gameLog = gameLog;
            },
            buyProperty: function () {
                connection.invoke("buyProperty");
            },
            betOnAuction: function () {
                connection.invoke("betOnAuction", this.betAmount_txt || 0);
            },
            startAuction: function () {
                connection.invoke("startAuctionOnProperty");
            },
            endTurn: function () {
                this.createTradeInProgress = false;
                this.propertySelectionInProgress = false;
                connection.invoke("endTurn");
            },
            getTileClasses: function (tile) {
                var tileClass = "";

                if (tile.ownerPlayerID) {
                    tileClass += " Player-Background-" +
                        this.gameState.players.filter(p => p.id == tile.ownerPlayerID)[0].color;
                }
                if (this.propertySelectionInProgress) {
                    if (tile.ownerPlayerID == this.gameID &&
                        (this.canMortgageTile(tile) && this.propertySelectionType == "MortgageProperty") ||
                        (tile.isMortgaged && tile.ownerPlayerID == this.gameID && this.propertySelectionType == "RedeemProperty") ||
                        (this.canBuildOnTile(tile) && this.propertySelectionType == "BuildHouse") ||
                        (tile.ownerPlayerID == this.gameID && tile.buildingCount > 0 && this.propertySelectionType == "SellHouse")) {
                        tileClass += " selectable";
                    } else {
                        tileClass += " unselectable";
                    }
                }
                if (tile.type == "ColorProperty") {
                    tileClass += " property";
                } else if (tile.type == "Chance") {
                    tileClass += " chance";
                } else if (tile.type == 'Railroad') {
                    tileClass += " railroad";
                } else if (tile.name == "Income Tax") {
                    tileClass += " fee income-tax";
                } else if (tile.name == "Luxury Tax") {
                    tileClass += " fee luxury-tax";
                } else if (tile.type == "CommunityChest") {
                    tileClass += " community-chest";
                }

                return tileClass;
            },
            getPlayerTextColorClass: function (player) {
                var playerTurnClass = "";

                return "Player-Text-" + player.color + " " + playerTurnClass;
            },
            buyOutOfJail: function () {
                connection.invoke("buyOutOfJail");
            },
            useGetOutOfJailFreeCard: function () {
                connection.invoke("useGOOJFC");
            },
            startBuildHouse: function (event) {
                event.stopPropagation();
                if (this.propertySelectionType == "BuildHouse" && this.propertySelectionInProgress) {
                    this.propertySelectionInProgress = false;
                    return;
                }
                this.propertySelectionInProgress = true;
                this.tileClickAction = this.buildHouse;
                this.propertySelectionType = "BuildHouse";
            },
            startSellHouse: function (event) {
                event.stopPropagation();
                if (this.propertySelectionType == "SellHouse" && this.propertySelectionInProgress) {
                    this.propertySelectionInProgress = false;
                    return;
                }
                this.propertySelectionInProgress = true;
                this.tileClickAction = this.sellHouse;
                this.propertySelectionType = "SellHouse";
            },
            startMortgageProperty: function (event) {
                event.stopPropagation();
                if (this.propertySelectionType == "MortgageProperty" && this.propertySelectionInProgress) {
                    this.propertySelectionInProgress = false;
                    return;
                }
                this.propertySelectionInProgress = true;
                this.tileClickAction = this.mortgageProperty;
                this.propertySelectionType = "MortgageProperty";
            },
            startRedeemProperty: function (event) {
                event.stopPropagation();
                if (this.propertySelectionType == "RedeemProperty" && this.propertySelectionInProgress) {
                    this.propertySelectionInProgress = false;
                    return;
                }
                this.propertySelectionInProgress = true;
                this.tileClickAction = this.redeemProperty;
                this.propertySelectionType = "RedeemProperty";
            },
            tileClicked: function (event) {
                event.stopPropagation();
                if (!this.gameInProgress || !this.propertySelectionInProgress) {
                    return;
                }
                var tile = event.currentTarget;
                var tileID = parseInt(tile.id);

                if (tileID < 0 || tileID >= 40)
                    return;

                if (this.gameState.tiles[tileID].ownerPlayerID == this.gameID) {
                    this.tileClickAction(tileID);
                }
            },
            centerClicked: function (event) {
                if (this.propertySelectionType && this.propertySelectionInProgress) {
                    this.propertySelectionInProgress = false;
                }
            },
            buildHouse: function (tileIndex) {
                connection.invoke("buildHouse", tileIndex);
            },
            sellHouse: function (tileIndex) {
                connection.invoke("sellHouse", tileIndex);
            },
            mortgageProperty: function (tileIndex) {
                connection.invoke("mortgageProperty", tileIndex);
            },
            redeemProperty: function (tileIndex) {
                connection.invoke("redeemProperty", tileIndex);
            },
            instantMonopoly: function (tileIndex) {
                connection.invoke("instantMonopoly");
            },
            getLogEntryClass: function (entry) {
                let playerID = extractGuid(entry);
                if (playerID) {
                    var player = this.gameState.players.find(x => x.id == playerID);
                    if (player)
                        return "Player-Text-" + player.color;
                }
                return "";
            },
            declareBankruptcy: function () {
                connection.invoke("declareBankruptcy");
            },
            startCreateTrade: function () {
                this.tradeTargetPlayerID = this.otherPlayers[0].id;
                this.createTradeInProgress = true;
            },
            cancelCreateTrade: function () {
                this.createTradeInProgress = false;
                this.clearTradeOfferData();
            },
            sendTrade: function () {
                connection.invoke("offerTrade",
                    this.tradeTargetPlayerID,
                    this.selectedTradeMyPlayerProperties,
                    this.selectedTradeTargetPlayerProperties,
                    this.selectedTradeMyPlayerMoney,
                    this.selectedTradeTargetPlayerMoney);
                this.createTradeInProgress = false;
                this.clearTradeOfferData();
            },
            acceptTrade: function (tradeOfferID) {
                connection.invoke("acceptTrade", tradeOfferID);
                if (!this.playerHasTradeOffers) {
                    this.disableShowTradeOffers();
                }
            },
            rejectTrade: function (tradeOfferID) {
                connection.invoke("rejectTrade", tradeOfferID);
                if (!this.playerHasTradeOffers) {
                    this.disableShowTradeOffers();
                }
            },
            clearTradeOfferData: function () {
                this.selectedTradeTargetPlayerProperties = [];
                this.selectedTradeTargetPlayerMoney = 0;
                this.selectedTradeMyPlayerProperties = [];
                this.selectedTradeMyPlayerMoney = 0;
            },
            updatePlayerPositions: function (ignoreLogic = false) {
                if (!this.gameInProgress || !this.gameState.players || this.gameState.players.length == 0 || this.animationInProgress)
                    return;

                //Wait for initial game state to load in Vue:
                for (var i = 0; i < this.gameState.players.length; i++) {
                    var player = this.gameState.players[i];

                    if (this.currentUIBoardPositions[player.id] === undefined) {
                        //initial game movement
                        //console.log("initial player movement");
                        this.movePlayerDirect(player, player.boardPosition);
                        continue;
                    }

                    if (player.wasDirectMovement && player.boardPosition != this.currentUIBoardPositions[player.id]) {
                        //console.log("direct movement");
                        if (player.prevBoardPosition != 30 && player.isInJail
                            && !this.gameState.chanceDeck.currentPlayerCardText
                            && !this.gameState.communityChestDeck.currentPlayerCardText) {
                            this.createEventPopup("Jail", player.name + " got sent to jail for overspeeding!", 2000);
                        }
                        this.movePlayerDirect(player, player.boardPosition);
                    } else if (player.boardPosition != this.currentUIBoardPositions[player.id]) {
                        //Typical movement:
                        //console.log("normal movement");
                        this.movePlayerClockwise(player, player.boardPosition);
                    } else if (ignoreLogic) {
                        //assume window update:
                        //console.log("window update player position");
                        this.movePlayerDirect(player, player.boardPosition, 1, true);
                    }
                }
            },
            createMovementEventPopups: function (player, currentBoardPosition, targetBoardPosition) {
                if (targetBoardPosition == currentBoardPosition
                    && this.gameState.tiles[targetBoardPosition].type == 'Chance'
                    && this.gameState.chanceDeck.currentPlayerCardText) {
                    this.createEventPopup("Chance", this.gameState.chanceDeck.currentPlayerCardText, 2000);
                }
                if (targetBoardPosition == currentBoardPosition
                    && this.gameState.tiles[targetBoardPosition].type == 'CommunityChest'
                    && this.gameState.communityChestDeck.currentPlayerCardText) {
                    this.createEventPopup("Community Chest", this.gameState.communityChestDeck.currentPlayerCardText, 2000);
                }

                var playerIdx = this.gameState.players.indexOf(player);
                //var playerPrevState = (playerIdx != -1 && this.prevGameState.players) ? this.prevGameState.players[playerIdx] : null;
                if (player.prevBoardPosition != 0 && currentBoardPosition == 0) {
                    this.createEventPopup("Salary", player.name + " collected $200 for passing Go!", 1500);
                }

                if (targetBoardPosition == 30 && currentBoardPosition == 30) {
                    console.log("GO TO JAIL", this.gameState.currentTile.type);
                    this.createEventPopup("Jail", player.name + " got sent to jail!", 2000);
                }
            },
            createEventPopup: function (title, message, duration, cssClass = "") {
                let eventPopup = {};
                eventPopup.title = title;
                eventPopup.message = message;
                eventPopup.duration = duration;
                eventPopup.id = "event_popup_" + uuidv4();
                eventPopup.class = cssClass;

                this.eventPopupQueue.push(eventPopup) - 1; //.push() returns length of new array

                setTimeout(function (_this, event) {
                    $("#" + event.id).fadeOut("slow", function () {
                        _this.eventPopupQueue = _this.eventPopupQueue.filter(x => x.id != event.id);
                    });
                }, eventPopup.duration, this, eventPopup);
            },
            movePlayerClockwise: function (player, targetBoardPosition) {
                var currentBoardPosition = this.currentUIBoardPositions[player.id] || 0;

                for (var iter = 0; iter < 41; iter++) {
                    this.animationInProgress = true;

                    var targetPos = this.getPlayerTilePos(player, currentBoardPosition);
                    this.animatePlayerMovement(player, targetPos, 110, currentBoardPosition, targetBoardPosition);

                    if (currentBoardPosition == targetBoardPosition)
                        break;

                    currentBoardPosition++;
                    if (currentBoardPosition >= 40)
                        currentBoardPosition = 0;
                }

                this.prevUIBoardPositions[player.id] = this.currentUIBoardPositions[player.id] || 0;
                this.currentUIBoardPositions[player.id] = targetBoardPosition;
                //console.log("Reached target board position! (prev, curr)", this.prevUIBoardPositions[player.id], this.currentUIBoardPositions[player.id]);
            },
            movePlayerDirect: function (player, targetBoardPosition, animationLength = 1000, ignoreLogic = false) {
                var playerDiv = $("#player_token_" + player.id);
                var targetPos = this.getPlayerTilePos(player, targetBoardPosition);

                if (ignoreLogic) {
                    playerDiv.css(targetPos);
                    this.animationInProgress = false;
                    return;
                }

                this.animatePlayerMovement(player, targetPos, animationLength, targetBoardPosition, targetBoardPosition);

                this.prevUIBoardPositions[player.id] = this.currentUIBoardPositions[player.id] || 0;
                this.currentUIBoardPositions[player.id] = targetBoardPosition;
            },
            getPlayerTilePos: function (player, targetBoardPosition) {
                let nextTilePos = $('#' + targetBoardPosition).offset();

                let posOffset = { left: 0, top: 0 };
                let playersOnSameSpot = this.gameState.players.filter(x => x.id != player.id && x.boardPosition == targetBoardPosition)
                if (playersOnSameSpot && playersOnSameSpot.length > 0) {
                    let playerPriority = this.gameState.players.indexOf(player) + 1;
                    posOffset.left = 6 * playerPriority;
                    posOffset.top = 3 * playerPriority;
                }
                if (player.isInJail && targetBoardPosition == 10) {
                    posOffset.left += 30;
                }

                return {
                    left: nextTilePos.left + posOffset.left,
                    top: nextTilePos.top + posOffset.top
                };
            },
            animatePlayerMovement: function (player, pos, animationDuration, currentBoardPosition, targetBoardPosition) {
                var div = $("#player_token_" + player.id);
                let args = [player, currentBoardPosition, targetBoardPosition];

                this.animationInProgress = true;

                div.animate(
                    {
                        left: pos.left,
                        top: pos.top
                    },
                    animationDuration,
                    () => {
                        window.clearTimeout(this.animationTimeoutHandle);
                        this.animationTimeoutHandle = setTimeout(() => {
                            this.animationInProgress = false;
                        }, animationDuration * 1.1);

                        this.createMovementEventPopups(args[0], args[1], args[2]);
                    });
            },
            canBuildOnTile: function (tile) {
                var canBuild = false;

                if (tile.color && tile.ownerPlayerID == this.gameID && this.ownedProperties.length > 1 && tile.buildingCount < 5) {
                    let coloredProperties = this.gameState.tiles.filter(x => x.type == 'ColorProperty').groupBy('color');
                    canBuild = coloredProperties[tile.color].every(prop => prop.ownerPlayerID == this.gameID && !prop.isMortgaged);
                }

                return canBuild;
            },
            canMortgageTile: function (tile) {
                var canMortgage = !tile.isMortgaged &&
                    (!tile.buildingCount || tile.buildingCount == 0);

                if (tile.color && tile.ownerPlayerID == this.gameID && this.ownedProperties.length > 1) {
                    let coloredProperties = this.gameState.tiles.filter(x => x.type == 'ColorProperty').groupBy('color');
                    if (coloredProperties[tile.color].some(prop => prop.buildingCount > 0))
                        canMortgage = false;
                }

                return canMortgage;
            },
            collapseLobbyBar: function () {
                this.lobbyBarCollapsed = !this.lobbyBarCollapsed;
            },
            checkForNotifications: function () {
                if (!this.prevGameState || !this.gameState)
                    return;
                if (this.prevGameState.auction && !this.gameState.auction) {
                    var auctionedProperty = this.gameState.tiles.find(t => t.id == this.prevGameState.currentTile.id);
                    var auctionWinner = this.gameState.players.find(x => x.id == auctionedProperty.ownerPlayerID);
                    var auctionWinnerBetAmount = this.prevGameState.players.find(x => x.id == auctionWinner.id).money - auctionWinner.money;
                    var msg = "Player '" + auctionWinner.name + "' won auction for property '" + auctionedProperty.name + "' for $" +
                        auctionWinnerBetAmount + "!";
                    this.createEventPopup("Auction Finished", msg, 2000);
                }
            },
            setInfoTile: function (event, tile) {
                event.stopPropagation();

                if (tile && tile.mortgageValue) {
                    this.infoTile = tile;
                } else {
                    this.infoTile = null;
                }
            },
            toggleSound: function () {
                this.soundEnabled = !this.soundEnabled;
            },
            onPlayerTurnStart: function () {
                if (this.soundEnabled) {
                    document.getElementById('sound_knock').play();
                }
            },
            rejectedTrade: function (tradeID) {
                var trade = this.gameState.tradeOffers.find(x => x.id == tradeID);
                let playerName = "Player";
                if (trade != null) {
                    let p = this.gameState.players.find(x => x.id == trade.playerB.id);
                    if (p != null) {
                        playerName = p.name;
                    }
                }
                this.createEventPopup("Trade Rejected!", playerName + " rejected your trade!", 25000, "trade-rejected-event-popup");
            },
            acceptedTrade: function () {
                var trade = this.gameState.tradeOffers.find(x => x.id == tradeID);
                let playerName = "Player";
                if (trade != null) {
                    let p = this.gameState.players.find(x => x.id == trade.playerB.id);
                    if (p != null) {
                        playerName = p.name;
                    }
                }
                this.createEventPopup("Trade Rejected!", playerName + " accepted your trade!", 25000, "trade-accepted-event-popup");
            },
            processGameStateQueue: function () {
                if (this.processingGameState)
                    return;
                this.processingGameState = true;

                while (this.gameStateQueue.length) {
                    var tGameState = this.gameStateQueue.shift();
                    //console.log("processing game state", tGameState);
                    this.prevGameState = this.gameState;
                    this.gameState = tGameState;

                    if (this.animationInProgress) {
                        break;
                    }
                    this.$nextTick(() => {
                        this.updatePlayerPositions();
                    });
                }

                this.processingGameState = false;
                requestAnimationFrame(this.processGameStateQueue);
            }
        }
    })

    //====================//
    //SignalR 2 setup:
    // var lobbyHub = $.connection.lobbyHub;

    //.net core client:
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/lobbyhub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("updateState", (state) => {
        console.log("Received new state:", state);
        vueApp.state = state;
        if (state.player != null) {
            vueApp.updatePlayer(state.player);
        }

        if (state.lobbies.some(lobby => lobby.owner.computerUserID == vueApp.playerID)) {
            var ownedLobby = state.lobbies.filter(lobby => lobby.owner.computerUserID == vueApp.playerID)[0];
            vueApp.lobbyName = ownedLobby.name;
        }
    });

    connection.on("rejectedTrade", vueApp.rejectedTrade);
    connection.on("acceptedTrade", vueApp.acceptedTrade);

    connection.on("updateGameState", (gameState) => {
        console.log("NEW GAME STATE: ", gameState);
        if (!vueApp.gameInProgress) {
            //first game state:
            vueApp.lobbyBarCollapsed = true;
            requestAnimationFrame(vueApp.processGameStateQueue);
        }
        if (gameState.players.length == 0) {
            vueApp.disconnectFromLobby();
            return;
        }

        vueApp.gameStateQueue.push(gameState);

        if (vueApp.prevGameState
            && vueApp.prevGameState.currentPlayer
            && vueApp.prevGameState.currentPlayer.id != vueApp.gameState.currentPlayer.id
            && vueApp.gameState.currentPlayer.id == vueApp.gameID) {
            vueApp.onPlayerTurnStart();
        }
        vueApp.checkForNotifications();
    });

    connection.on("updateGameLog", vueApp.updateGameLog);

    //START THE HUB:
    async function start() {
        try {
            await connection
                .start()
                .then(function () {
                    Main();
                });
        } catch (err) {
            console.error("SignalR connection failed: " + err);
            console.log("retrying connection in 5 seconds...");
            setTimeout(() => start(), 5000);
        }
    };

    connection.onclose(async () => {
        await start();
    });

    start();

    //$.connection.hub.url = "/lobbyhub";
    //$.connection.hub.start().done(function () {
    //    Main();
    //}).fail(function (error) {
    //    console.log("SignalR connection failed: " + error);
    //    //setTimeout(() => window.location.href = '', 7000);
    //});

    //====================//
    //Main:
    function Main() {
        console.log("lobby hub connected!");
        $(window).on("unload", vueApp.disconnectFromHub);
        $(window).on('beforeunload', () => {
            vueApp.disconnectFromHub();
            return;
        });
        $(window).on("resize", function () { vueApp.updatePlayerPositions(true); });

        AutoLogin();

        if (window.test) {
            window.test(vueApp);
        }
    }

    function AutoLogin() {
        let localData = vueApp.getLocalStorageData();
        vueApp.username = localData.username;
        vueApp.registerPlayer();
    }

    window.vueApp = vueApp;
});