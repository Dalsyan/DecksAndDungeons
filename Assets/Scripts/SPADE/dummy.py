import time
import spade

class DummyAgent(spade.agent.Agent):
    async def setup(self):
        print("Hello World! I'm agent {}".format(str(self.jid)))

if __name__ == "__main__":
    print("hello")
    dummy = DummyAgent("pvidal@lightwitch.org", "Pepelxmpp11,")
    print("how r u")
    future = dummy.start()
    print("ayayay")
    future.result()

    while dummy.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    dummy.stop()
