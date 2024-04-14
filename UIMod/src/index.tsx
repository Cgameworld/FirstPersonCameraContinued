import { ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";

const register: ModRegistrar = (moduleRegistry) => {

    const CustomMenuButton = () => {
        return <div>
            <button id="MapTextureReplacer-MainGameButton" className="button_ke4 button_ke4 button_h9N" onClick={() => trigger("map_texture", "MainWindowCreate")}>
                <div className="tinted-icon_iKo icon_be5" style={{ backgroundImage: 'url(coui://uil/Standard/VideoCamera.svg)', backgroundPositionX: '2rem', backgroundPositionY: '2rem', backgroundColor: 'rgba(255,255,255,0)', backgroundSize: '35rem 35rem' }}>
                </div>
            </button>
        </div>;
    }

    moduleRegistry.append('GameTopRight', CustomMenuButton);
}

export default register;