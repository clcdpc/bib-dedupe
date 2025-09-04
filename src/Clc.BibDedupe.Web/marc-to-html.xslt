<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:marc="http://www.loc.gov/MARC21/slim"
  exclude-result-prefixes="marc">
  <xsl:output method="html" encoding="utf-8" omit-xml-declaration="yes" indent="no"/>
  <xsl:strip-space elements="*"/>

  <xsl:template match="/">
    <div class="marc-records">
      <xsl:apply-templates select="//marc:record"/>
    </div>
  </xsl:template>

  <xsl:template match="marc:record">
    <table class="marc-table marc-compact">
      <colgroup>
        <col class="col-tag"/>
        <col class="col-ind"/>
        <col class="col-ind"/>
        <col class="col-data"/>
      </colgroup>
      <thead>
        <tr>
          <th>Tag</th>
          <th>Ind1</th>
          <th>Ind2</th>
          <th>Data</th>
        </tr>
      </thead>
      <tbody>
        <!-- Leader -->
        <tr>
          <td class="tag">
            <span class="t" data-marc-tag="LDR">LDR</span>
          </td>
          <td class="ind">
            <span class="i" data-marc-tag="LDR" data-ind-pos="1" data-ind-val=" ">
              <xsl:text>&#160;</xsl:text>
            </span>
          </td>
          <td class="ind">
            <span class="i" data-marc-tag="LDR" data-ind-pos="2" data-ind-val=" ">
              <xsl:text>&#160;</xsl:text>
            </span>
          </td>
          <td class="data">
            <div class="wrap">
              <span class="val">
                <xsl:value-of select="normalize-space(marc:leader)"/>
              </span>
            </div>
          </td>
        </tr>

        <!-- Controlfields -->
        <xsl:for-each select="marc:controlfield">
          <tr>
            <td class="tag">
              <span class="t" data-marc-tag="{@tag}">
                <xsl:value-of select="@tag"/>
              </span>
            </td>
            <td class="ind">
              <span class="i" data-marc-tag="{@tag}" data-ind-pos="1" data-ind-val=" ">
                <xsl:text>&#160;</xsl:text>
              </span>
            </td>
            <td class="ind">
              <span class="i" data-marc-tag="{@tag}" data-ind-pos="2" data-ind-val=" ">
                <xsl:text>&#160;</xsl:text>
              </span>
            </td>
            <td class="data">
              <div class="wrap">
                <span class="val">
                  <xsl:value-of select="."/>
                </span>
              </div>
            </td>
          </tr>
        </xsl:for-each>

        <!-- Datafields; no separators or extra spaces -->
        <xsl:for-each select="marc:datafield">
          <tr>
            <td class="tag">
              <span class="t" data-marc-tag="{@tag}">
                <xsl:value-of select="@tag"/>
              </span>
            </td>

            <td class="ind">
              <span class="i" data-marc-tag="{@tag}" data-ind-pos="1" data-ind-val="{@ind1}">
                <xsl:choose>
                  <xsl:when test="not(@ind1) or @ind1=' '">
                    <xsl:text>&#160;</xsl:text>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="@ind1"/>
                  </xsl:otherwise>
                </xsl:choose>
              </span>
            </td>

            <td class="ind">
              <span class="i" data-marc-tag="{@tag}" data-ind-pos="2" data-ind-val="{@ind2}">
                <xsl:choose>
                  <xsl:when test="not(@ind2) or @ind2=' '">
                    <xsl:text>&#160;</xsl:text>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="@ind2"/>
                  </xsl:otherwise>
                </xsl:choose>
              </span>
            </td>

            <td class="data">
              <div class="wrap">
                <xsl:for-each select="marc:subfield">
                  <xsl:element name="span">
                    <xsl:attribute name="class">sf</xsl:attribute>
                    <xsl:attribute name="data-marc-tag">
                      <xsl:value-of select="../@tag"/>
                    </xsl:attribute>
                    <xsl:attribute name="data-marc-code">
                      <xsl:value-of select="@code"/>
                    </xsl:attribute>

                    <xsl:element name="span">
                      <xsl:attribute name="class">code</xsl:attribute>
                      <xsl:text>$</xsl:text>
                      <xsl:value-of select="@code"/>
                    </xsl:element>

                    <xsl:element name="span">
                      <xsl:attribute name="class">val</xsl:attribute>
                      <xsl:value-of select="normalize-space(.)"/>
                    </xsl:element>
                  </xsl:element>
                </xsl:for-each>
              </div>
            </td>
          </tr>
        </xsl:for-each>

      </tbody>
    </table>
  </xsl:template>
</xsl:stylesheet>
