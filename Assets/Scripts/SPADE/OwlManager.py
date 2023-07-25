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

    async def listen_for_messages(self):
        self.owl_socket.listen(5)
        
        while True:
            try:
                client_socket, address = self.owl_socket.accept()
                print(f'Conectado a cliente: {client_socket}')

                message = client_socket.recv(1024*3).decode("utf-8")
                print(f"message: {message}")
                
                if not message:
                    break

                if message == "close":
                    await self.close_action()

                else:
                    try:
                        message_dict = json.loads(message)
                        print(f"message_dict: {message_dict}.")

                        action = message_dict.get("action")
                        data = message_dict.get("data")
                        player = message_dict.get("player")

                        if action == "showDeck":
                            self.show_deck(player, data)

                        elif action == "showCards":
                            self.show_cards(player, data)
                            
                        elif action == "showDeckCards":
                            self.show_deck_cards(player, data)

                        else:
                            print("Unknown action:", action)
                            break

                    except json.JSONDecodeError:
                        print(f"Invalid message format: {message}")
                        
                print(f"He cerrado conexion con: {self.spade_socket}")
                client_socket.close()

            except Exception as e:
                print("Error listening for message:", str(e))

    def show_deck(self, name):
        deck = self.actions.search_for_decks(name)
    
    def show_cards(self):
        pass
    
    def show_deck_cards(self):
        pass

if __name__ == "__init__":
    print("listo para escuchar")