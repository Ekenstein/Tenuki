import { BrowserRouter, Route, Routes } from 'react-router'
import { GameSessionManagerProvider } from '../features/game/session/components/GameSessionManagerProvider'
import { SignalRProvider } from '../infrastructure/signalr/components/SignalRProvider'
import './App.css'
import { CreateGamePage } from '../features/game/pages/CreateGamePage'
import { GamePage } from '../features/game/pages/GamePage'

function App() {
  console.log("App called")
  return (
    <>
        <BrowserRouter>
          <SignalRProvider>
            <GameSessionManagerProvider>
              <section id="center">
                <Routes>
                  <Route path="/" element={<CreateGamePage />} />
                  <Route path="/games/:gameId" element={<GamePage />} />
                </Routes>
              </section>
            </GameSessionManagerProvider>
          </SignalRProvider>
        </BrowserRouter>
    </>
  )
}

export default App
