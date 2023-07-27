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
        self.owl_socket.listen(5)
        print("escuchando mensajes!")
        
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
                        password = message_dict.get("password")

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

                                response = "logged"
                                byte_response = (response).encode()
                                client_socket.sendall(bytearray(byte_response))
                                print(f"enviado: {response}")

                        elif action == "showDeck":
                            self.show_deck(data)

                        elif action == "showCards":
                            self.show_cards(data)
                            
                        elif action == "showDeckCards":
                            self.show_deck_cards(data)

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

    def show_deck(self, name):
        deck = self.actions.search_for_decks(name)
    
    def show_cards(self):
        pass
    
    def show_deck_cards(self):
        pass

    def close_action(self):
        self.owl_socket.close()

if __name__ == "__main__":
    print("estoy en el MAIN")
    owl_manager = OwlManager()
    asyncio.run(owl_manager.listen_for_messages())