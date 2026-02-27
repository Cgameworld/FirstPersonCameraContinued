import React, { useState } from 'react';
import { trigger } from "cs2/api";

// ---- EDIT CHANGELOG CONTENT HERE ----
const CHANGELOG_TITLE = "v1.6 Update!";

const CHANGELOG_HIGHLIGHTS = [
    { heading: "Main New Features:", items: [
        "Added dynamic strip map display when following transit vehicles",
        "Added toggleable picture-in-picture overlay (press p)",
        "Added zoom mode (press z)",
    ]},
];

const CHANGELOG_FULL = [
    { heading: "Improvements:", items: [
        "Fixed lag when looking at ground",
        "Free Camera view now stays above water",
        "Follow Random Bicycle mode no longer picks parked bikes",
    ]},
    { heading: "Other Changes:", items: [
        "Vehicle type hidden on default in infobox",
        "Increased transition speed factor speed default",
        "Removed first person shortcut when line info panel is selected",
    ]},
];
// ---- END CHANGELOG CONTENT ----

interface ChangelogSection {
    heading: string;
    items: string[];
}

const SectionList: React.FC<{ sections: ChangelogSection[] }> = ({ sections }) => (
    <>
        {sections.map((section, i) => (
            <div key={i} style={i > 0 ? { marginTop: '10rem' } : {}}>
                <p className="p_CKq" style={{ fontWeight: 'bold', marginBottom: '5rem' }}>{section.heading}</p>
                {section.items.map((item, j) => (
                    <p className="p_CKq" key={j}>- {item}</p>
                ))}
            </div>
        ))}
    </>
);

const ChangelogWindow: React.FC = () => {
    const [showMore, setShowMore] = useState(false);

    const onClose = () => {
        trigger("fpc", "DismissChangelog");
    };

    return (
        <div
            style={{
                position: "fixed",
                top: 0,
                left: 0,
                width: "100%",
                height: "85%",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                zIndex: 9999,
            }}
        >
            <div className="panel_YqS error-dialog_iaV" style={{ maxWidth: "100%", maxHeight: "100%", width: '490rem' }}>
                <div className="header_jAe header_Bpo child-opacity-transition_nkS">
                    <div className="title-bar_PF4">
                        <div className="icon-space_h_f"></div>
                        <div className="title_ctd title_zQN">First Person Camera Continued</div>
                        <button className="button_bvQ button_bvQ close-button_wKK" onClick={onClose}>
                            <div className="tinted-icon_iKo icon_PhD" style={{ maskImage: "url(Media/Glyphs/Close.svg)" }}></div>
                        </button>
                    </div>
                </div>
                <div className="content_VBF content_AD7 child-opacity-transition_nkS">
                    <div className="icon-layout_cZT row_L6K">
                        <div className="main-column_Jzk">
                            <div className="error-message_r4_">
                                <div className="paragraphs_nbD">
                                    <p className="p_CKq" style={{ fontSize: '18rem', fontWeight: 'bold', marginBottom: '10rem' }}>{CHANGELOG_TITLE}</p>
                                    <SectionList sections={CHANGELOG_HIGHLIGHTS} />

                                    {!showMore && (
                                        <div style={{ marginTop: '12rem' }}>
                                            <button
                                                className="button_HeP button_gJo"
                                                style={{ width: 'auto', padding: '4rem 12rem' }}
                                                onClick={() => setShowMore(true)}
                                            >
                                                Show Full Changelog ▼
                                            </button>
                                        </div>
                                    )}

                                    {showMore && (
                                        <>
                                            <div style={{ marginTop: '12rem' }}>
                                                <button
                                                    className="button_HeP button_gJo"
                                                    style={{ width: 'auto', padding: '4rem 12rem' }}
                                                    onClick={() => setShowMore(false)}
                                                >
                                                    Hide Full Changelog ▲
                                                </button>
                                            </div>
                                            <div style={{ marginTop: '10rem' }}>
                                                <SectionList sections={CHANGELOG_FULL} />
                                            </div>
                                        </>
                                    )}
                                </div>
                            </div>
                            <div className="buttons-container" style={{ marginTop: '20rem', marginRight: '10rem', textAlign: 'right' }}>
                                <div className="buttons_lZi row_L6K" style={{ width: '175rem' }}>
                                    <button className="button_HeP button_gJo" style={{ width: "125rem" }} onClick={onClose}>Ok</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ChangelogWindow;
