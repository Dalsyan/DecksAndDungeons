import json
import random
import sys
import socket as s

from owlready2 import *
from spade.agent import Agent
from spade.behaviour import *

import Actions
import AgenteCarta

##############################
#                            #
#          CONSTANTS         #
#                            #
##############################

OFFLINE = "OFFLINE"
ONLINE = "ONLINE"

SELECT_DECKS = "SELECT_DECKS"
PREPARE_DECKS = "PREPARE_DECKS"
GAME_START = "GAME_START"
PLAYER_PLAY_CARDS = "PLAYER_PLAY_CARDS"
ENEMY_PLAY_CARDS = "ENEMY_PLAY_CARDS"
CARD_ACTIONS = "CARD_ACTIONS"
GAME_OVER = "GAME_OVER"

FREE = False
OCCUPIED = True

##############################
#                            #
#          MANAGER           #
#                            #
##############################

class AgentManager(Agent):
    async def setup(self):
        print("Estoy en el SETUP")

        # TIPO DE PARTIDA
        self.game_type = OFFLINE

        # LISTAS CON AGENTES
        self.card_agents = []
        self.player_card_agents = []
        self.enemy_card_agents = []

        # MAZOS Y TABLERO
        self.player_deck = None
        self.enemy_deck = None
        self.table = {}

        # WIN CONDITIONS
        self.player_score = 0
        self.enemy_score = 0
        self.winner = None

        # FLAGS
        self.listening = False
        self.player_ready = False
        self.enemy_ready = False
        
        # INICIALIZACION TABLERO
        for i in range(6):
            for j in range(6):
                self.table[f"({i}, {j})"] = FREE

        # CREAR SERVER DE SPADE
        self.spade_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.spade_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        self.spade_socket.bind(('localhost', 8001))
        
        # CONECTARME A SERVER DE UNITY
        self.unity_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.unity_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        self.unity_socket.connect(('localhost', 8000))
        print(f"Conectado a {self.unity_socket}")

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions(self.spade_socket, self.unity_socket)

        behav = ManagerBehav()

        # ESTADOS
        #behav.add_state(name = SELECT_DECKS, state = SelectDecks(), initial = True)
        behav.add_state(name = PREPARE_DECKS, state = PrepareDecks(), initial = True)
        behav.add_state(name = GAME_START, state = GameStart())
        behav.add_state(name = PLAYER_PLAY_CARDS, state = PlayerPlayCards())
        behav.add_state(name = ENEMY_PLAY_CARDS, state = EnemyPlayCards())
        behav.add_state(name = CARD_ACTIONS, state = CardActions())
        behav.add_state(name = GAME_OVER, state = GameOver())

        # TRANSICIONES
        #behav.add_transition(source = SELECT_DECKS, dest = GAME_START)
        behav.add_transition(source = PREPARE_DECKS, dest = GAME_START)
        behav.add_transition(source = GAME_START, dest = PLAYER_PLAY_CARDS)
        behav.add_transition(source = PLAYER_PLAY_CARDS, dest = ENEMY_PLAY_CARDS)
        behav.add_transition(source = ENEMY_PLAY_CARDS, dest = CARD_ACTIONS)
        behav.add_transition(source = CARD_ACTIONS, dest = PLAYER_PLAY_CARDS)
        behav.add_transition(source = CARD_ACTIONS, dest = GAME_OVER)
        
        self.add_behaviour(behav)

    ##############################
    #                            #
    #          LISTENER          #
    #                            #
    ##############################

    async def listen_for_messages(self):
        self.listening = True
        self.spade_socket.listen(5)
        print(f"Escuchando en {self.spade_socket}")

        while self.listening:
            try:
                client_socket, address = self.spade_socket.accept()
                print(f'Conectado a cliente: {client_socket}')

                message = client_socket.recv(1024*3).decode("utf-8")
                print(f"message: {message}")
                
                if not message:
                    break

                if message == "close":
                    await self.close_action()

                elif message == "playerReady":
                    await self.ready_action("player")
                    self.listening = False
                            
                elif message == "enemyReady":
                    await self.ready_action("enemy")
                    self.listening = False

                else:
                    try:
                        message_dict = json.loads(message)
                        print(f"message_dict: {message_dict}.")

                        action = message_dict.get("action")
                        data = message_dict.get("data")
                        
                        if action == "selectPlayerDeck":
                            await self.select_deck_action("player", data)

                            if self.player_deck is not None and self.enemy_deck is not None:
                                self.listening = False

                        elif action == "selectEnemyDeck":
                            await self.select_deck_action("enemy", data)

                            if self.player_deck is not None and self.enemy_deck is not None:
                                self.listening = False

                        elif action == "createPlayerCard":
                            await self.play_card_action("player", data)

                        elif action == "createEnemyCard":
                            await self.play_card_action("enemy", data)

                        else:
                            print("Unknown action:", action)
                            break

                    except json.JSONDecodeError:
                        print(f"Invalid message format: {message}")
                        
                print(f"He cerrado conexion con: {self.spade_socket}")
                client_socket.close()

            except Exception as e:
                print("Error listening for message:", str(e))

    ##############################
    #                            #
    #      LISTENER_ACTIONS      #
    #                            #
    ##############################

    async def select_deck_action(self, owner, name):
        deck = self.actions.search_for_deck(name)

        if owner == "player":
            self.player_deck = deck
            print(f"player_deck: {deck.name}")

        elif owner == "enemy":
            self.enemy_deck = deck
            print(f"enemy_deck: {deck.name}")
            
        print(self.actions.deck_to_json_action(deck))

    async def play_card_action(self, owner, name):
        card = self.actions.search_for_card(name)
        print(self.actions.card_to_json_action(card))

        card_agent = AgenteCarta.CardAgent(f"{card.name}@lightwitch.org", "Pepelxmpp11,", card)
        self.card_agents.append(card_agent)

        if owner == "player":
            self.player_card_agents.append(card_agent)

        elif owner == "enemy":
            self.enemy_card_agents.append(card_agent)

    async def ready_action(self, owner):
        if owner == "player":
            self.player_ready = True

        elif owner == "enemy":
            self.enemy_ready = True

    async def close_action(self):
        if self.spade_socket:
            self.spade_socket.close()
            print(f"unity socket: {self.unity_socket}")

        self.unity_socket.close()
        print(f"unity socket: {self.unity_socket}")

        for agent in self.card_agents:
            await agent.stop()

        await self.stop()
        print("exiting")

        sys.exit()

##############################
#                            #
#          BEHAVIOUR         #
#                            #
##############################

class ManagerBehav(FSMBehaviour):        
    async def on_start(self):
        print("El behaviour ha empezado!")

    async def on_end(self):
        await self.agent.close_action()

##############################
#                            #
#           STATES           #
#                            #
##############################

class SelectDecks(State):
    async def run(self):
        print("State: SELECT_DECKS")

        await self.agent.actions.send_message_to_socket("select_decks")
        await self.agent.listen_for_messages()

        print("State TO: PREPARE_DECKS")
        self.set_next_state(PREPARE_DECKS)

class PrepareDecks(State):
    async def run(self):
        print("State: PREPARE_DECKS")

        player_deck = self.agent.actions.create_deck()
        enemy_deck = self.agent.actions.create_deck()
        
        player_deck_json = self.agent.actions.deck_to_json_action("create_player_deck", player_deck)
        enemy_deck_json = self.agent.actions.deck_to_json_action("create_enemy_deck", enemy_deck)

        await self.agent.actions.send_action_to_socket(player_deck_json)
        time.sleep(1)
        await self.agent.actions.send_action_to_socket(enemy_deck_json)
        time.sleep(1)
        
        print("State TO: GAME_START")
        self.set_next_state(GAME_START)

class GameStart(State):
    async def run(self):
        print("State: GAME_START")

        if self.agent.game_type == ONLINE:
            player_initiative = random.randint(1,20)
            enemy_initiative = random.randint(1,20)

            initiative_action = {} 
            initiative_action["action"] = "first_turn"

            if player_initiative >= enemy_initiative:
                initiative_action["data"] = "player"
                await self.agent.actions.send_action_to_socket(initiative_action)
            else:
                initiative_action["data"] = "enemy"
                await self.agent.actions.send_action_to_socket(initiative_action)
            time.sleep(1)

        await self.agent.actions.send_message_to_socket("start_game")

        print("State TO: PLAYER_PLAY_CARDS")
        self.set_next_state(PLAYER_PLAY_CARDS)

class PlayerPlayCards(State):
    async def run(self):
        print("State: PLAYER_PLAY_CARDS")

        play_cards = {}
        play_cards["action"] = "player_play_cards"

        if self.agent.winner == "player":
            print("State TO: GAME_OVER")
            self.set_next_state(GAME_OVER)
        
        play_cards["data"] = True
        await self.agent.actions.send_action_to_socket(play_cards)
        time.sleep(1)

        await self.agent.listen_for_messages()

        while not self.agent.player_ready:
            pass

        self.agent.player_ready = False
        play_cards["data"] = False
        await self.agent.actions.send_action_to_socket(play_cards)
        time.sleep(1)

        print("State TO: ENEMY_PLAY_CARDS")
        self.set_next_state(ENEMY_PLAY_CARDS)

class EnemyPlayCards(State):
    async def run(self):
        print("State: ENEMY_PLAY_CARDS")

        play_cards = {}
        play_cards["action"] = "enemy_play_cards"

        if self.agent.winner == "enemy":
            print("State TO: GAME_OVER")
            self.set_next_state(GAME_OVER)
        
        play_cards["data"] = True
        await self.agent.actions.send_action_to_socket(play_cards)
        time.sleep(1)

        await self.agent.listen_for_messages()

        while not self.agent.enemy_ready:
            pass

        self.agent.enemy_ready = False
        play_cards["data"] = False
        await self.agent.actions.send_action_to_socket(play_cards)
        time.sleep(1)

        print("State TO: CARD_ACTIONS")
        self.set_next_state(CARD_ACTIONS)

class CardActions(State):
    async def run(self):
        print("State: CARD_ACTIONS")
        
        for agent in self.agent.card_agents:
            agent.player_card_agents = self.agent.player_card_agents
            agent.enemy_card_agents = self.agent.enemy_card_agents
            await agent.start()

            sent_msg = Message(to = f'{agent.name}@lightwitch.org')
            sent_msg.body = "start"
            await self.send(sent_msg)

            recv_msg = await self.receive(10)
            if recv_msg:
                if recv_msg.body == "stop":
                    await agent.stop()

                else: 
                    print("no he recibido nada")
                    await agent.stop()

            while agent.is_alive():
                pass


        #if len(self.agent.player_card_agents) == 0 or len(self.agent.player_card_agents) < len(self.agent.enemy_card_agents):
        #    self.agent.enemy_score += 1

        #elif len(self.agent.enemy_card_agents) == 0 or len(self.agent.player_card_agents) > len(self.agent.enemy_card_agents):
        #    self.agent.player_score += 1

        #if self.agent.player_score >= 2:
        #    self.agent.winner = "player"
        #elif self.agent.enemy_score >= 2:
        #    self.agent.winner = "enemy"

        print("State TO: PLAYER_PLAY_CARDS")
        self.set_next_state(PLAYER_PLAY_CARDS)

class GameOver(State):
    async def run(self):
        print("State: GAME_OVER")
        
##############################
#                            #
#            MAIN            #
#                            #
##############################

if __name__ == "__main__":
    manager = AgentManager("pvidal_manager@lightwitch.org", "Pepelxmpp11,")
    future = manager.start()
    future.result()

    while manager.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    manager.stop()