import json
import sys
import socket as s

from owlready2 import *
from spade.agent import Agent
from spade.behaviour import *

import Actions
import AgenteCarta

##############################
#                            #
#          MANAGER           #
#                            #
##############################

PREPARE_DECKS = "PREPARE_DECKS"
GAME_START = "GAME_START"
PLAYER_PLAY_CARDS = "PLAYER_PLAY_CARDS"
ENEMY_PLAY_CARDS = "ENEMY_PLAY_CARDS"
CARD_ACTIONS = "CARD_ACTIONS"
GAME_OVER = "GAME_OVER"

class AgentManager(Agent):
    async def setup(self):
        self.card_agents = []
        self.player_card_agents = []
        self.enemy_card_agents = []
        self.listening = False
        self.start_game = False
        print("Estoy en el SETUP")
        
        # CREAR SERVER DE SPADE
        self.spade_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.spade_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        
        # CONECTARME A SERVER DE UNITY
        self.unity_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.unity_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        self.unity_socket.connect(('localhost', 8000))
        print(f"Conectado a {self.unity_socket}")

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions(self.spade_socket, self.unity_socket)

        behav = ManagerBehav()

        # ESTADOS
        behav.add_state(name = PREPARE_DECKS, state = PrepareDecks(), initial = True)
        behav.add_state(name = GAME_START, state = GameStart())
        behav.add_state(name = PLAYER_PLAY_CARDS, state = PlayerPlayCards())
        behav.add_state(name = ENEMY_PLAY_CARDS, state = EnemyPlayCards())
        behav.add_state(name = CARD_ACTIONS, state = CardActions())
        behav.add_state(name = GAME_OVER, state = GameOver())

        # TRANSICIONES
        behav.add_transition(source = PREPARE_DECKS, dest = GAME_START)
        behav.add_transition(source = GAME_START, dest = GAME_START)
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
        self.spade_socket.bind(('localhost', 8001))
        self.spade_socket.listen(5)
        print(f"Escuchando en {self.spade_socket}")

        while self.listening:
            try:
                client_socket, address = self.spade_socket.accept()
                print(f'Conectado a cliente: {client_socket}')
                message = client_socket.recv(1024*3).decode("utf-8")
                
                if not message:
                    break

                if message == "close":
                    print(f"message: {message}")
                    await self.close_action()
                    
                elif message == "start":
                    print(f"message: {message}")
                    print(f'start_action: {self.start_game}')
                    await self.start_action()
                    print(f'start_action: {self.start_game}')
                    self.listening = False

                else:
                    try:
                        message_dict = json.loads(message)
                        print(f"message_dict: {message_dict}.")
                        action = message_dict.get("action")
                        print(f"action: {action}.")
                        data = message_dict.get("data")
                        print(f"data: {data}")
                        
                        if str(action) == "createPlayerCard":
                            await self.create_card_action("player", data)

                        elif str(action) == "createEnemyCard":
                            self.create_card_action("enemy", data)

                        else:
                            print("Unknown action:", action)
                            break
                    except json.JSONDecodeError:
                        print(f"Invalid message format: {message}")
                        
                print(f"He cerrado conexion con: {self.spade_socket}")
                client_socket.close()
            except Exception as e:
                print("Error listening for messages:", str(e))

    async def close_action(self):
        if self.spade_socket:
            self.spade_socket.close()
        print(f"unity socket: {self.unity_socket}")
        self.unity_socket.close()
        print(f"unity socket: {self.unity_socket}")
        await self.stop()
        print("exiting")
        sys.exit()

    async def start_action(self):
        print("start_action")
        self.start_game = True

    async def create_card_action(self, owner, name):
        card = self.actions.search_for_card(name)
        print(self.actions.card_to_json_action(card))

        card_agent = AgenteCarta.CardAgent(f'{card.name}@lightwitch.org', "Pepelxmpp11,",
                                          card.cclass, card.race, owner, 
                                          card.level, card.hp, card.ac, card.strength, card.con, card.dex, card.damage, card.magic, card.rango, card.prio, card.pos)

        self.card_agents.append(card_agent)
        if owner == "player":
            self.player_card_agents.append(card_agent)
        elif owner == "enemy":
            self.enemy_card_agents.append(card_agent)

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
#          ESTADOS           #
#                            #
##############################

class PrepareDecks(State):
    async def run(self):
        print("State: PREPARE_DECKS")
        player_deck = self.agent.actions.create_deck()
        player_deck = self.agent.actions.deck_to_json_action("create_player_deck", player_deck)
        await self.agent.actions.send_action_to_socket(player_deck)

        enemy_deck = self.agent.actions.create_deck()
        enemy_deck = self.agent.actions.deck_to_json_action("create_enemy_deck", enemy_deck)
        await self.agent.actions.send_action_to_socket(enemy_deck)
        
        print("State TO: GAME_START")
        self.set_next_state(GAME_START)

class GameStart(State):
    async def run(self):
        print("State: GAME_START")
        await self.agent.listen_for_messages()
        while not self.agent.start_game:
            print(self.agent.start_game)
        self.agent.spade_socket.close()
        self.set_next_state(PLAYER_PLAY_CARDS)

class PlayerPlayCards(State):
    async def run(self):
        print("Player 1 PLAY YOUR CARDS")
        while True:
            pass

class EnemyPlayCards(State):
    async def run(self):
        print("Player 2 PLAY YOUR CARDS")

class CardActions(State):
    async def run(self):
        print("Let the cards do their things")

class GameOver(State):
    async def run(self):
        print("THE GAME IS OVER")
        
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