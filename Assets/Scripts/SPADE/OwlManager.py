import asyncio
import random
import socket as s
import json

from owlready2 import *

import Actions

ADDRESS = 'localhost'
PORT = 8002

class OwlManager:
    actions = Actions.Actions()
    
    owl_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
    owl_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
    owl_socket.bind((ADDRESS, PORT))
    print(f"conectado a ({ADDRESS}, {PORT})")

    async def listen_for_messages(self):
        listening = True
        self.owl_socket.listen(5)
        print("escuchando mensajes!")
        
        while listening:
            try:
                client_socket, address = self.owl_socket.accept()
                print(f'Conectado a cliente: {client_socket}')

                message = client_socket.recv(1024*3).decode("utf-8")
                print(f"message: {message}")
                
                if not message:
                    break

                if message == "close":
                    listening = False
                    self.close_action()

                else:
                    try:
                        message_dict = json.loads(message)
                        print(f"message_dict: {message_dict}.")

                        action = message_dict.get("action")
                        data = message_dict.get("data")
                        password = message_dict.get("password")
                        page = message_dict.get("page")
                        
                        if action == "registerUser":
                            user = self.create_user(data, password)
                            print(f'user: {user.name}')

                            if user is not None:
                                print(f"user created: {user.name}")

                                response = "registered"
                                byte_response = (response).encode()
                                client_socket.sendall(bytearray(byte_response))

                        elif action == "loginUser":
                            user = self.login_user(data, password)

                            if user:
                                print(f"user: {user.name} logged")

                                response_json = {"action" : "logged", "data" : {"wins" : int(user.wins), "loses" : int(user.loses)}}
                                byte_response_json = json.dumps(response_json).encode()
                                client_socket.sendall(byte_response_json)

                        elif action == "showDecks":
                            decks = self.show_decks("dalso")
                            
                            if decks is not None:
                                deck_json = {"action" : "show_decks", "data" : decks}
                                byte_deck_json = (json.dumps(deck_json)).encode()

                                client_socket.sendall(bytearray(byte_deck_json))

                        elif action == "showCards":
                            cards = self.show_cards("dalso", page)

                            if cards is not None:
                                card_json = {'action': 'show_cards', 'data': cards}
                                byte_card_json = json.dumps(card_json).encode()

                                client_socket.sendall(bytearray(byte_card_json))
                            
                        elif action == "createDeck":
                            deck = self.actions.create_player_deck("dalso")

                        elif action == "showDeckCards":
                            cards = self.show_deck_cards(data)

                            if cards is not None:
                                card_json = {'action': 'show_deck_cards', 'data': cards}
                                byte_card_json = json.dumps(card_json).encode()

                                client_socket.sendall(bytearray(byte_card_json))

                        else:
                            print("Unknown action:", action)
                            break

                    except json.JSONDecodeError:
                        print(f"Invalid message format: {message}")
                        
                print(f"He cerrado conexion con: {self.owl_socket}")
                client_socket.close()

            except Exception as e:
                print("Error listening for message:", str(e))

    def create_user(self, name, password):
        user = self.actions.create_user(name, password)
        return user

    def login_user(self, name, password):
        user = self.actions.login_user(name, password)
        return user

    def show_decks(self, name):
        decks = self.actions.search_for_decks(name)
        print(decks)
        return decks
    
    def show_cards(self, name, page):
        cards = self.actions.search_for_cards(name, page)
        return cards
    
    def show_deck_cards(self, deck):
        return self.actions.search_for_deck_cards(deck)

    def close_action(self):
        self.owl_socket.close()

if __name__ == "__main__":
    print("estoy en el MAIN")
    owl_manager = OwlManager()
    asyncio.run(owl_manager.listen_for_messages())