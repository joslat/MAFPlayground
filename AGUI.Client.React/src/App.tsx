import { CopilotKit } from "@copilotkit/react-core";
import { CopilotSidebar } from "@copilotkit/react-ui";
import "@copilotkit/react-ui/styles.css";
import "./index.css";
import { RecipeDemo } from "./samples/SharedStateCookingSimple/RecipeDemo";

function App() {
  // Cache-busting: Add version parameter to force refresh
  const runtimeUrl = "http://localhost:8888/?v=" + Date.now();
  
  return (
    <CopilotKit 
      runtimeUrl={runtimeUrl}
      showDevConsole={true}
    >
      <CopilotSidebar
        defaultOpen={true}
        clickOutsideToClose={false}
        labels={{
          title: "Recipe Assistant",
          initial: "Hi! I'm your Recipe Assistant. Ask me to modify ingredients, instructions, or dietary preferences!"
        }}
      >
        <RecipeDemo />
      </CopilotSidebar>
    </CopilotKit>
  );
}

export default App;
